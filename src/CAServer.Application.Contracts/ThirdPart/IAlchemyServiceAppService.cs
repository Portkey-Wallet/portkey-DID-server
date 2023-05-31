using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using JetBrains.Annotations;

namespace CAServer.ThirdPart;

public interface IAlchemyServiceAppService
{
    Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemyFiatListDto> GetAlchemyFiatListAsync();
    Task<AlchemyCryptoListDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
    Task<AlchemySignatureResultDto> GetAlchemySignatureV2Async(object input, [CanBeNull] List<string> ignoreProperties);
}