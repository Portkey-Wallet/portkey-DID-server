using System;
using CAServer.Account;
using CAServer.CAAccount.Enums;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Portkey.Contracts.CA;
using Volo.Abp;

namespace CAServer.CAAccount.Strategy;

public class TonWalletVerificationStrategy : CAServerAppService, IVerificationAlgorithmStrategy
{
    public VerifierType VerifierType => VerifierType.TonWallet;
    
    private readonly ILogger<TonWalletVerificationStrategy> _logger;
    public TonWalletVerificationStrategy(ILogger<TonWalletVerificationStrategy> logger)
    {
        _logger = logger;
    }

    // public VerificationExt Converter(VerificationDo verificationDo)
    // {
    //     if (verificationDo?.VerificationDetails == null)
    //     {
    //         return new VerificationExt();
    //     }
    //
    //     return new VerificationExt()
    //     {
    //         TonVerification = new TonWalletVerification()
    //         {
    //             Address = verificationDo.VerificationDetails.Address,
    //             PublicKey = verificationDo.VerificationDetails.PublicKey,
    //             Signature = verificationDo.VerificationDetails.Signature,
    //             Timestamp = new Timestamp()
    //             {
    //                 Seconds = verificationDo.VerificationDetails.Timestamp,
    //                 Nanos = 0
    //             },
    //             Extra = verificationDo.VerificationDetails.Extra,
    //         }
    //     };
    // }

    public string ExtraHandler(string salt, string message)
    {
        if (salt.IsNullOrEmpty() || message.IsNullOrEmpty())
        {
            throw new UserFriendlyException("ton wallet salt and message is invalid");
        }
        return string.Join(",", salt, message);
    }
}