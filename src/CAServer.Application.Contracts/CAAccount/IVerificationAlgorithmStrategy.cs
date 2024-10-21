using CAServer.Account;
using CAServer.CAAccount.Enums;
using Portkey.Contracts.CA;

namespace CAServer.CAAccount;

public interface IVerificationAlgorithmStrategy
{
    VerifierType VerifierType { get; }
    
    // public VerificationExt Converter(VerificationDo verificationDo);

    public string ExtraHandler(string salt, string message);
}