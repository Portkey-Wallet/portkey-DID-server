using CAServer.Verifier;

namespace CAServer.CAAccount.Cmd;

public class VerifySecondaryEmailCmd
{
    public string SecondaryEmail { get; set; }
    
    public PlatformType PlatformType { get; set; }
}