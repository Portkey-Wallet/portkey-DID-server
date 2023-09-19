namespace CAServer;

public class CAServerApplicationConsts
{
    public const string MessageStreamName = "CAServer";
    
    public const string EmailRegex = @"[\w!#$%&'*+/=?^_`{|}~-]+(?:\.[\w!#$%&'*+/=?^_`{|}~-]+)*@(?:[\w](?:[\w-]*[\w])?\.)+[\w](?:[\w-]*[\w])?";
    public const string PhoneRegex = @"^1[3456789]\d{9}$";
    public const string ChooseVerifierServerErrorMsg = "A problem occurred when assigning a decentralized verifier. Please try again.";
}