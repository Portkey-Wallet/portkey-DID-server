using System.Threading.Tasks;
using CAServer.Message.Dtos;
using CAServer.Message.Etos;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Message;

[RemoteService(false), DisableAuditing]
public class MessageAppService : CAServerAppService, IMessageAppService
{
    private readonly IDistributedEventBus _distributedEventBus;

    public MessageAppService(IDistributedEventBus distributedEventBus)
    {
        _distributedEventBus = distributedEventBus;
    }

    public async Task ScanLoginSuccessAsync(ScanLoginDto request)
    {
        var eto = ObjectMapper.Map<ScanLoginDto, ScanLoginEto>(request);
        eto.MethodName = "onScanLoginSuccess";
        await _distributedEventBus.PublishAsync(eto);
    }

    public async Task ScanLoginAsync(ScanLoginDto request)
    {
        var eto = ObjectMapper.Map<ScanLoginDto, ScanLoginEto>(request);
        eto.MethodName = "onScanLogin";
        await _distributedEventBus.PublishAsync(eto);
    }
}