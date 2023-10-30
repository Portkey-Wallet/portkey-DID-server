using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;

namespace CAServer.ThirdPart;

public interface IAlchemyServiceAppService
{
    Task<CommonResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<CommonResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input);
    Task<List<AlchemyFiatDto>> GetAlchemyNftFiatListAsync();
    Task<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    Task<CommonResponseDto<AlchemySignatureResultDto>> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
    Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input);
}