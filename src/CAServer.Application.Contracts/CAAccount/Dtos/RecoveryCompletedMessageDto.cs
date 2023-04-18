using CAServer.Account;

namespace CAServer.CAAccount.Dtos;

public class RecoveryCompletedMessageDto : AccountCompletedMessageBase
{
    public string RecoveryStatus { get; set; }
    public string RecoveryMessage { get; set; }
}