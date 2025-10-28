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
    Task<CommonResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListWithCacheAsync(GetAlchemyFiatListDto input);
    Task<(List<AlchemyFiatDto>, string)> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input);
    Task<List<AlchemyFiatDto>> GetAlchemyNftFiatListAsync();
    Task<CommonResponseDto<List<AlchemyCryptoDto>>> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<CommonResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    Task<CommonResponseDto<AlchemySignatureResultDto>> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
    Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input);
}