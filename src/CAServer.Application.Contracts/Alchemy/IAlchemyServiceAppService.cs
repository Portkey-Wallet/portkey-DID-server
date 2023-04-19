using System.Threading.Tasks;
using CAServer.Alchemy.Dtos;

namespace CAServer.Alchemy;

public interface IAlchemyServiceAppService
{
    Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyFiatListDto> GetAlchemyFiatListAsync();
    Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
}