using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyServiceAppService
{
    Task<AlchemyTokenResponseDto> GetAlchemyFreeLoginTokenAsync(GetAlchemyFreeLoginTokenDto input);
    Task<AlchemySignatureResponseDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input);
    
    [Obsolete("For compatibility with old front-end versions.")]
    Task<AlchemyFiatListResponseDto> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input);
    
    [Obsolete("For compatibility with old front-end versions.")]
    Task<AlchemyCryptoListResponseDto> GetAlchemyCryptoListAsync(GetAlchemyCryptoListDto input);
    
    [Obsolete("For compatibility with old front-end versions.")]
    Task<AlchemyOrderQuoteResponseDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input);
    
}