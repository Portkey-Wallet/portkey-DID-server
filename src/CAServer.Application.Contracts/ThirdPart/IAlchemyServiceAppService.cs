using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyServiceAppService
{
    Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyFiatListDto> GetAlchemyFiatListAsync();
    Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
}