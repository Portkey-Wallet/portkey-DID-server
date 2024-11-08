using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.AddressBook.Dtos;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Transfer.Dtos;
using CAServer.Transfer.Proxy;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using GetNetworkListDto = CAServer.Transfer.Dtos.GetNetworkListDto;

namespace CAServer.Transfer;

[RemoteService(false), DisableAuditing]
public class TransferAppService : CAServerAppService, ITransferAppService
{
    private readonly IETransferProxyService _eTransferProxyService;
    private readonly DepositOptions _options;

    public TransferAppService(IETransferProxyService eTransferProxyService, IOptionsSnapshot<DepositOptions> options)
    {
        _eTransferProxyService = eTransferProxyService;
        _options = options.Value;
    }

    public async Task<AuthTokenDto> GetConnectTokenAsync(AuthTokenRequestDto request)
    {
        return await _eTransferProxyService.GetConnectTokenAsync(request);
    }

    public async Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request)
    {
        return await _eTransferProxyService.GetTokenListAsync(request);
    }

    public async Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(
        GetTokenOptionListRequestDto request)
    {
        return await _eTransferProxyService.GetTokenOptionListAsync(request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        return await _eTransferProxyService.GetNetworkListAsync(request);
    }

    public async Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(
        GetCalculateDepositRateRequestDto request)
    {
        return await _eTransferProxyService.CalculateDepositRateAsync(request);
    }

    public async Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        var depositInfoWrap = await _eTransferProxyService.GetDepositInfoAsync(request);
        if (depositInfoWrap.Code != ETransferConstant.SuccessCode)
        {
            return depositInfoWrap;
        }

        var depositInfo = depositInfoWrap.Data;
        var count = GetConfirmations(request.Network, depositInfo);

        var symbol = request.Symbol == ETransferConstant.SgrName ? ETransferConstant.SgrDisplayName : request.Symbol;
        var toSymbol = request.ToSymbol == ETransferConstant.SgrName
            ? ETransferConstant.SgrDisplayName
            : request.ToSymbol;
        depositInfo.DepositInfo.ExtraNotes = _options.ExtraNotes;
        depositInfo.DepositInfo.ExtraNotes[0] =
            depositInfo.DepositInfo.ExtraNotes[0].Replace("{BlockNumber}", count.ToString());
        depositInfo.DepositInfo.ExtraNotes[1] =
            depositInfo.DepositInfo.ExtraNotes[1].Replace("{FromToken}", symbol);

        if (request.Symbol != request.ToSymbol)
        {
            depositInfo.DepositInfo.ExtraNotes.AddRange(_options.SwapExtraNotes);
            depositInfo.DepositInfo.ExtraNotes[2] =
                depositInfo.DepositInfo.ExtraNotes[2].Replace("{FromToken}", symbol);
            depositInfo.DepositInfo.ExtraNotes[3] = depositInfo.DepositInfo.ExtraNotes[3]
                .Replace("{FromToken}", request.Symbol)
                .Replace("{ToToken}", toSymbol);
        }

        return depositInfoWrap;
    }

    private int GetConfirmations(string network, GetDepositInfoDto depositInfo)
    {
        var count = ETransferConstant.DefaultConfirmBlock;

        var reflectionInfo = _options.Reflection.GetOrDefault(network);
        if (reflectionInfo == null)
        {
            return count;
        }

        var extraNodes = depositInfo.DepositInfo.ExtraNotes.Where(t => t.Contains(reflectionInfo.Include));
        foreach (var extraNote in extraNodes)
        {
            var infoStr = extraNote.TrimEnd('.', ',', ' ');
            var words = infoStr.Split(' ').ToList();
            if (int.TryParse(words[words.IndexOf(reflectionInfo.NextWord) - 1], out var confirmCount))
            {
                count = confirmCount;
                break;
            }
        }

        return count;
    }

    public async Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        return await _eTransferProxyService.GetNetworkTokensAsync(request);
    }

    public async Task<ResponseWrapDto<PagedResultDto<OrderIndexDto>>> GetRecordListAsync(
        GetOrderRecordRequestDto request)
    {
        var skipCount = request.SkipCount;
        var maxResultCount = request.MaxResultCount;

        request.SkipCount = ETransferConstant.DefaultSkipCount;
        request.MaxResultCount = ETransferConstant.DefaultMaxResultCount;
        var wrapDto = await _eTransferProxyService.GetRecordListAsync(request);
        if (wrapDto.Code != ETransferConstant.SuccessCode)
        {
            return wrapDto;
        }

        var orders = wrapDto.Data;
        var items = orders.Items
            .Where(t => t.FromTransfer.Symbol == request.FromSymbol && t.FromTransfer.ToAddress == request.Address)
            .ToList();

        wrapDto.Data.Items = items.Skip(skipCount).Take(maxResultCount).ToList();
        wrapDto.Data.TotalCount = items.Count;

        return wrapDto;
    }

    public Task<GetSupportNetworkDto> GetSupportNetworkListAsync()
    {
        return Task.FromResult(new GetSupportNetworkDto()
        {
            SupportedNetworks = new Dictionary<string, Dictionary<string, List<NetworkBasicInfo>>>
            {
                ["AELF"] = new Dictionary<string, List<NetworkBasicInfo>>
                {
                    ["ELF"]=new List<NetworkBasicInfo>()
                    {
                        new NetworkBasicInfo()
                        {
                            Name = "ETH",
                            Network = "Ethereum"
                        }
                    }
                }
            }
        });
    }
}