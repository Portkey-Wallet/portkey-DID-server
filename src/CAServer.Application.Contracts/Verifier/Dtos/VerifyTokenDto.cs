using System;

namespace CAServer.Verifier.Dtos;

public class VerifyTokenDto<T>
{
    public string VerificationDoc{ get; set; }
    public string Signature{ get; set; }
    public T UserExtraInfo { get; set; }
}


public class TelegramUserExtraInfo
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string AuthDate { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Hash { get; set; }
    public string ProtoUrl { get; set; }
    public string GuardianType { get; set; }
    public DateTime AuthTime { get; set; }
}