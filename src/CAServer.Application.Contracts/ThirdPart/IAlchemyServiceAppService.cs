using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyServiceAppService
{
    Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyBaseResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input);
    Task<List<AlchemyFiatDto>> GetAlchemyNftFiatListAsync();
    Task<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
    Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input);
}