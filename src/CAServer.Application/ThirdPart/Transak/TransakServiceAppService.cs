using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Provider;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Transak;

public class TransakServiceAppService : ITransakServiceAppService, ISingletonDependency
{
    private readonly TransakProvider _transakProvider;

    public TransakServiceAppService(TransakProvider transakProvider)
    {
        _transakProvider = transakProvider;
    }

    public async Task<Tuple<string, string>> GetAccessTokenAsync()
    {
        return Tuple.Create(
            _transakProvider.GetApiKey(),
            await _transakProvider.GetAccessTokenAsync()
        );
    }
}