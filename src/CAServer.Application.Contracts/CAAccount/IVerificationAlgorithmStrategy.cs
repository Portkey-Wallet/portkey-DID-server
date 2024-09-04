using CAServer.Account;
using CAServer.CAAccount.Enums;
using Portkey.Contracts.CA;

namespace CAServer.CAAccount;

public interface IVerificationAlgorithmStrategy
{
    VerificationType VerificationType { get; }
    
    public VerificationExt Converter(VerificationDo verificationDo);

    public string ExtraHandler(string salt, string address = null);
}