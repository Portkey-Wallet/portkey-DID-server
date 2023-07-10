using CAServer.Message.Dtos;
using Xunit;

namespace CAServer.Message;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class MessageTest : CAServerApplicationTestBase
{
    private readonly IMessageAppService _messageAppService;

    public MessageTest()
    {
        _messageAppService = GetService<IMessageAppService>();
    }

    [Fact]
    public void ScanLoginSuccessTest()
    {
        _messageAppService.ScanLoginSuccessAsync(new ScanLoginDto
        {
            TargetClientId = "test-client-id"
        });
    }
    
    [Fact]
    public void ScanLoginTest()
    {
        _messageAppService.ScanLoginAsync(new ScanLoginDto
        {
            TargetClientId = "test-client-id"
        });
    }
}