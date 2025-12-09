using CAServer.Account;

namespace CAServer.Dtos;

public class RegisterCompletedMessageDto : AccountCompletedMessageBase
{
    public string RegisterStatus { get; set; }
    public string RegisterMessage { get; set; }
}