using System.Threading.Tasks;
using CAServer.Grains;
using CAServer.Grains.Grain.QrCode;
using CAServer.QrCode.Dtos;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.QrCode;

[RemoteService(false), DisableAuditing]
public class QrCodeAppService : CAServerAppService, IQrCodeAppService
{
    private readonly IClusterClient _clusterClient;

    public QrCodeAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<bool> CreateAsync(QrCodeRequestDto input)
    {
        var grainId = GrainIdHelper.GenerateGrainId("QrCode", input.Id);
        var grain = _clusterClient.GetGrain<IQrCodeGrain>(grainId);
        var result = await grain.AddIfAbsent();

        return result;
    }
}