using System;

namespace CAServer.Verifier;

public class SendVerificationRequestInput : VerifierServerBase
{
    public Guid VerifierSessionId { get; set; }

    public string ChainId { get; set; }


}