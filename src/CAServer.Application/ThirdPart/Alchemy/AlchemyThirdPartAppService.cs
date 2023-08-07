using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart.Alchemy;

public partial class AlchemyServiceAppService
{
    
    public string MerchantName()
    {
        return MerchantNameType.Alchemy.ToString();
    }
    
    public Task<AlchemyFiatListResponseDto> GetMerchantFiatAsync(GetAlchemyFiatListDto input)
    {
        //TODO
        throw new NotImplementedException();
    }

    public Task<AlchemyCryptoListResponseDto> GetMerchantCryptoAsync(GetAlchemyCryptoListDto input)
    {
        //TODO
        throw new NotImplementedException();
    }

    public Task<AlchemyOrderQuoteResponseDto> GetMerchantPriceAsync(GetAlchemyOrderQuoteDto input)
    {
        //TODO
        throw new NotImplementedException();
    }
}