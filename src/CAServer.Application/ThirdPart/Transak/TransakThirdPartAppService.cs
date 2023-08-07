using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Transak;

[RemoteService(false), DisableAuditing]
public class TransakThirdPartAppService : CAServerAppService, IThirdPartAppService, ITransakServiceAppService
{
    private readonly TransakProvider _transakProvider;

    public TransakThirdPartAppService(TransakProvider transakProvider)
    {
        _transakProvider = transakProvider;
    }

    public string MerchantName()
    {
        return MerchantNameType.Transak.ToString();
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