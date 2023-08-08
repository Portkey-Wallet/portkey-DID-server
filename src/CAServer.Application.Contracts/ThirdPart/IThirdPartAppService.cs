using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart;

public interface IThirdPartAppService
{
    string MerchantName();
    Task<QueryFiatResponseDto> GetMerchantFiatAsync(QueryFiatRequestDto input);
    Task<QueryCryptoResponseDto> GetMerchantCryptoAsync(QueryCurrencyRequestDto input);
    Task<QueryPriceResponseDto> GetMerchantPriceAsync(QueryPriceRequestDto input);
}