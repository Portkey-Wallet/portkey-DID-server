using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart;

public interface IThirdPartAppService
{
    string MerchantName();
    Task<AlchemyFiatListResponseDto> GetMerchantFiatAsync(GetAlchemyFiatListDto input);
    Task<AlchemyCryptoListResponseDto> GetMerchantCryptoAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyOrderQuoteResponseDto> GetMerchantPriceAsync(GetAlchemyOrderQuoteDto input);
}