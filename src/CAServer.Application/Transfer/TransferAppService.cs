using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Transfer.Dtos;
using CAServer.Transfer.Proxy;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

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
        var count = 0;

        var extraNodes = depositInfo.DepositInfo.ExtraNotes.Where(t => t.Contains("confirmation"));
        foreach (var extraNote in extraNodes)
        {
            var infoStr = extraNote.TrimEnd('.', ' ');
            var nextWord = infoStr.Contains("network confirmations") ? "network" : "confirmation";
            var words = infoStr.Split(' ').ToList();
            if (int.TryParse(words[words.IndexOf(nextWord) - 1], out var confirmCount))
            {
                count = confirmCount;
                break;
            }
        }

        depositInfo.DepositInfo.ExtraNotes = _options.ExtraNotes;
        depositInfo.DepositInfo.ExtraNotes[0] =
            depositInfo.DepositInfo.ExtraNotes[0].Replace("{BlockNumber}", count.ToString());
        depositInfo.DepositInfo.ExtraNotes[1] =
            depositInfo.DepositInfo.ExtraNotes[1].Replace("{FromToken}", request.Symbol);

        if (request.Symbol != request.ToSymbol)
        {
            depositInfo.DepositInfo.ExtraNotes.AddRange(_options.SwapExtraNotes);
            depositInfo.DepositInfo.ExtraNotes[2] =
                depositInfo.DepositInfo.ExtraNotes[2].Replace("{FromToken}", request.Symbol);
            depositInfo.DepositInfo.ExtraNotes[3] = depositInfo.DepositInfo.ExtraNotes[3]
                .Replace("{FromToken}", request.Symbol)
                .Replace("{ToToken}", request.ToSymbol);
        }

        return depositInfoWrap;
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

        request.SkipCount = 0;
        request.MaxResultCount = 1000;

        var wrapDto = await _eTransferProxyService.GetRecordListAsync(request);
        if (wrapDto.Code != ETransferConstant.SuccessCode)
        {
            return wrapDto;
        }

        var orders = wrapDto.Data;
        var res = orders.Items
            .Where(t => t.FromTransfer.Symbol == request.FromSymbol && t.FromTransfer.ToAddress == request.Address)
            .ToList();

        wrapDto.Data.Items = res.Skip(skipCount).Take(maxResultCount).ToList();
        wrapDto.Data.TotalCount = res.Count;

        return wrapDto;
    }
}