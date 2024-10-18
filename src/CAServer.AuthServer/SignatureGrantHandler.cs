using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.Contract;
using CAServer.Dto;
using CAServer.Etos;
using CAServer.Model;
using CAServer.Signature;
using CAServer.Signature.Provider;
using Google.Protobuf;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Polly;
using Portkey.Contracts.CA;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace CAServer;

public class SignatureGrantHandler : ITokenExtensionGrant
{
    private IDistributedEventBus _distributedEventBus;
    private ILogger<SignatureGrantHandler> _logger;
    private IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "CAServer:Auth:SignatureGrantHandler:";
    private ISignatureProvider _signatureProvider;
    private IDistributedCache<string> _distributedCache;

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();
        var caHash = context.Request.GetParameter("ca_hash").ToString();
        var chainId = context.Request.GetParameter("chain_id").ToString();

        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, timestampVal, caHash, chainId);
        if (invalidParamResult != null)
        {
            return invalidParamResult;
        }

        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var timestamp = long.Parse(timestampVal);
        var address = Address.FromPublicKey(publicKey).ToBase58();

        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        var timeRangeConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<TimeRangeOption>>().Value;
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();

        if (time < DateTime.UtcNow.AddMinutes(-timeRangeConfig.TimeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRangeConfig.TimeRange))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                $"The time should be {timeRangeConfig.TimeRange} minutes before and after the current time.");
        }

        var hash = Encoding.UTF8.GetBytes(address + "-" + timestamp).ComputeHash();
        if (!AElf.Cryptography.CryptoHelper.VerifySignature(signature, hash, publicKey))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed.");
        }

        //Find manager by caHash
        var graphqlConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<GraphQLOption>>().Value;
        var chainOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<ChainOptions>>().Value;
        _signatureProvider = context.HttpContext.RequestServices.GetRequiredService<ISignatureProvider>();

        var managerCheck = await CheckAddressAsync(chainId, graphqlConfig.Url, caHash, address, chainOptions);
        if (!managerCheck.HasValue || !managerCheck.Value)
        {
            _logger.LogError(
                $"Manager validation failed. caHash:{caHash}, address:{address}, chainId:{chainId}");
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Manager validation failed.");
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        _distributedEventBus = context.HttpContext.RequestServices.GetRequiredService<IDistributedEventBus>();
        _distributedCache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache<string>>();
        var user = await userManager.FindByNameAsync(caHash);
        if (user == null)
        {
            var userId = Guid.NewGuid();
            var createUserResult = await CreateUserAsync(userManager, userId, caHash, chainId);
            if (!createUserResult)
            {
                return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Create user failed.");
            }

            user = await userManager.GetByIdAsync(userId);
        }
        await _distributedCache.SetAsync($"UserLoginHandler:{caHash}", user.Id.ToString());

        var userClaimsPrincipalFactory = context.HttpContext.RequestServices
            .GetRequiredService<IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
        claimsPrincipal.SetScopes("CAServer");
        claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
        claimsPrincipal.SetAudiences("CAServer");

        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimDestinationsManager>()
            .SetAsync(principal);

        await _distributedEventBus.PublishAsync(new UserLoginEto()
        {
            Id = user.Id,
            UserId = user.Id,
            CaHash = caHash,
            CreateTime = DateTime.UtcNow,
            FromCaServer = true
        });
        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string timestampVal, string caHash,
        string chainId)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter pubkey.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(timestampVal) || !long.TryParse(timestampVal, out var time) || time <= 0)
        {
            errors.Add("invalid parameter timestamp.");
        }

        if (string.IsNullOrWhiteSpace(caHash))
        {
            errors.Add("invalid parameter ca_hash.");
        }

        if (string.IsNullOrWhiteSpace(chainId))
        {
            errors.Add("invalid parameter chain_id.");
        }

        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }));
        }

        return null;
    }

    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
    }

    private async Task<bool> CreateUserAsync(IdentityUserManager userManager, Guid userId, string caHash, string chainId)
    {
        var result = false;
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + caHash);
        //get shared lock
        if (handle != null)
        {
            var user = new IdentityUser(userId, userName: caHash, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            var identityResult = await userManager.CreateAsync(user);

            if (identityResult.Succeeded)
            {
                _logger.LogInformation("Send create user event...");
                await _distributedEventBus.PublishAsync(new CreateUserEto
                {
                    Id = userId,
                    UserId = userId,
                    CaHash = caHash,
                    CreateTime = DateTime.UtcNow,
                    ChainId = chainId
                });

                _logger.LogDebug($"create user success: {userId.ToString()}");
            }

            result = identityResult.Succeeded;
        }
        else
        {
            _logger.LogError($"do not get lock, keys already exits. userId: {userId.ToString()}");
        }

        return result;
    }

    private async Task<bool?> CheckAddressAsync(string chainId, string graphQlUrl, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var graphQlResult = await CheckAddressFromGraphQlAsync(graphQlUrl, caHash, manager);
        if (graphQlResult.HasValue && graphQlResult.Value)
        {
            return true;
        }

        var contractResult = await CheckAddressFromContractAsync(chainId, caHash, manager, chainOptions);
        if (contractResult.HasValue && contractResult.Value)
        {
            return true;
        }
        var cacheKey = GetCacheKey(manager);
        var result = await _distributedCache.GetAsync(cacheKey);
        return !result.IsNullOrEmpty() && caHash.Equals(JsonConvert.DeserializeObject<ManagerCacheDto>(result)?.CaHash);
    }

    private string GetCacheKey(string manager)
    {
        return "Portkey:SocialRecover:" + manager;
    }

    private async Task<bool?> CheckAddressFromGraphQlAsync(string url, string caHash,
        string managerAddress)
    {
        var caHolderManagerInfo = await GetManagerList(url, caHash);
        var caHolderManager = caHolderManagerInfo?.CaHolderManagerInfo.FirstOrDefault();
        return caHolderManager?.ManagerInfos.Any(t => t.Address == managerAddress);
    }

    private async Task<bool?> CheckAddressFromContractAsync(string chainId, string caHash, string manager,
        ChainOptions chainOptions)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            LoginGuardianIdentifierHash = Hash.Empty
        };

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, MethodName.GetHolderInfo, param, false,
                chainOptions);

        return output?.ManagerInfos?.Any(t => t.Address.ToBase58() == manager);
    }

    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain, ChainOptions chainOptions) where T : class, IMessage<T>, new()
    {
        try
        {
            var chainInfo = chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
            var contractAddress = isCrossChain
                ? (await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(ContractName.CrossChain)))
                .ToBase58()
                : chainInfo.ContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, contractAddress,
                    methodName, param);
            var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());
            transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = transaction.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

            return value;
        }
        catch (Exception e)
        {
            if (methodName != MethodName.GetHolderInfo)
            {
                _logger.LogError(e, methodName + " error: {param}", param);
            }

            _logger.LogError("{Message}", e);
            return null;
        }
    }

    private async Task<CAHolderManagerInfo> GetManagerList(string url, string caHash)
    {
        try
        {
            using var graphQLClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());

            // It should just one item
            var testBlockRequest = new GraphQLRequest
            {
                Query =
                    "query{caHolderManagerInfo(dto: {skipCount:0,maxResultCount:10,caHash:\"" + caHash +
                    "\"}){chainId,caHash,caAddress,managerInfos{address,extraData}}}"
            };

            var graphQLResponse = await graphQLClient.SendQueryAsync<CAHolderManagerInfo>(testBlockRequest);
            return graphQLResponse.Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetManagerList graphQLClient error");
            return new CAHolderManagerInfo()
            {
                CaHolderManagerInfo = new List<CAHolderManager>()
            };
        }
    }

    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }));
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }

    public string Name { get; } = "signature";
}