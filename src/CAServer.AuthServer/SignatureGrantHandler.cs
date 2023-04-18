using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Dto;
using CAServer.Etos;
using CAServer.Model;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
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

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKey = ByteArrayHelper.HexStringToByteArray(context.Request.GetParameter("pubkey").ToString());
        var signature = ByteArrayHelper.HexStringToByteArray(context.Request.GetParameter("signature").ToString());
        var timestamp = long.Parse(context.Request.GetParameter("timestamp").ToString());
        var caHash = context.Request.GetParameter("cahash").ToString();
        var address = Address.FromPublicKey(publicKey).ToBase58();

        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        var timeRangeConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<TimeRangeOption>>().Value;
        
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
        
        // //Find manager by caHash
        var graphqlConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<GraphQLOption>>().Value;
        
        var managerAddressCheck = await CheckAddress(graphqlConfig.Url, caHash, address);
        if (!managerAddressCheck.HasValue || !managerAddressCheck.Value)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "ManagerAddress validation failed.");
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        _distributedEventBus = context.HttpContext.RequestServices.GetRequiredService<IDistributedEventBus>();

        var user = await userManager.FindByNameAsync(caHash);
        if (user == null)
        {
            var userId = Guid.NewGuid();
            var identityResult = await CreateUserAsync(userManager, userId, caHash, address);
            if (!identityResult.Succeeded)
            {
                return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Create user failed.");
            }

            user = await userManager.GetByIdAsync(userId);
        }

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

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private async Task<IdentityResult> CreateUserAsync(IdentityUserManager userManager, Guid userId, string caHash,
        string caAddress)
    {
        var user = new IdentityUser(userId, userName: caHash, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
        var identityResult = await userManager.CreateAsync(user);

        if (identityResult.Succeeded)
        {
            await _distributedEventBus.PublishAsync(new CreateUserEto
            {
                Id = userId,
                UserId = userId,
                CaAddress = caAddress,
                CaHash = caHash,
                CreateTime = DateTime.UtcNow
            });
        }

        return identityResult;
    }

    private async Task<bool?> CheckAddress(string url, string caHash, string managerAddress)
    {
        var caHolderManagerInfo = await GetManagerList(url, caHash);
        var caHolderManager = caHolderManagerInfo?.CaHolderManagerInfo.FirstOrDefault();
        return caHolderManager?.Managers.Any(t => t.Manager == managerAddress);
    }

    private async Task<CAHolderManagerInfo> GetManagerList(string url, string caHash)
    {
        using var graphQLClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());

        // It should just one item
        var testBlockRequest = new GraphQLRequest
        {
            Query =
                "query{caHolderManagerInfo(dto: {skipCount:0,maxResultCount:10,caHash:\"" + caHash +
                "\"}){chainId,caHash,caAddress,managers{manager,deviceString}}}"
        };

        var graphQLResponse = await graphQLClient.SendQueryAsync<CAHolderManagerInfo>(testBlockRequest);

        return graphQLResponse.Data;
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