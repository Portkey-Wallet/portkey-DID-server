using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using CAServer.Cache;
using AElf.Types;
using CAServer.ClaimToken.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Portkey.Contracts.TokenClaim;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.ClaimToken;

public class ClaimTokenAppService : IClaimTokenAppService, ISingletonDependency
{
    private readonly ClaimTokenWhiteListAddressesOptions _claimTokenWhiteListAddressesOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ICacheProvider _cacheProvider;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private const string GetClaimTokenAddressCacheKey = "CAServer:GetClaimTokenAddress:";


    public ClaimTokenAppService(
        IOptionsSnapshot<ClaimTokenWhiteListAddressesOptions> claimTokenWhiteListAddressesOptions,
        IContractProvider contractProvider, ICacheProvider cacheProvider,
        IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOptions)
    {
        _contractProvider = contractProvider;
        _cacheProvider = cacheProvider;
        _claimTokenInfoOption = claimTokenInfoOptions.Value;
        _claimTokenWhiteListAddressesOptions = claimTokenWhiteListAddressesOptions.Value;
    }

    public async Task<ClaimTokenResponseDto> GetClaimTokenAsync(ClaimTokenRequestDto claimTokenRequestDto)
    {
        var cacheClaimToken = await _cacheProvider.Get(GetClaimTokenAddressCacheKey + claimTokenRequestDto.Address);
        if (int.TryParse(cacheClaimToken, out var count))
        {
            if (count >= _claimTokenInfoOption.GetClaimTokenLimit)
            {
                throw new UserFriendlyException("Today's limit has been reached.");
            }
        }

        var address = _claimTokenWhiteListAddressesOptions.WhiteListAddresses.FirstOrDefault();
        if (address.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("No available address.");
        }

        var chainId = _claimTokenInfoOption.ChainId;

        var getBalanceParam = new GetBalanceInput
        {
            Symbol = claimTokenRequestDto.Symbol,
            Owner = Address.FromBase58(address)
        };

        var getBalanceOutput =
            await _contractProvider.CallTransactionAsync<GetBalanceOutput>(AElfContractMethodName.GetBalance,
                getBalanceParam, true, chainId);
        if (getBalanceOutput.Balance < _claimTokenInfoOption.ClaimTokenAmount)
        {
            var claimTokenParam = new ClaimTokenInput
            {
                Symbol = claimTokenRequestDto.Symbol,
                Amount = _claimTokenInfoOption.ClaimTokenAmount
            };
            await _contractProvider.SendTransactionAsync<ClaimTokenInput>(AElfContractMethodName.ClaimToken,
                claimTokenParam, chainId);
        }

        var transferParam = new TransferInput
        {
            Symbol = claimTokenRequestDto.Symbol,
            Amount = long.Parse(claimTokenRequestDto.Amount),
            To = Address.FromBase58(claimTokenRequestDto.Address)
        };

        var result =
            await _contractProvider.SendTransactionAsync<TransferInput>(AElfContractMethodName.Transfer, transferParam,
                chainId);

        await _cacheProvider.Increase(GetClaimTokenAddressCacheKey + claimTokenRequestDto.Address, 1,
            TimeSpan.FromHours(_claimTokenInfoOption.ExpireTime));
        return new ClaimTokenResponseDto
        {
            TransactionId = result.TransactionId
        };
    }
}