using System;
using Volo.Abp.EventBus;
namespace CAServer.Verifier.Etos;

[EventName("VerifierCodeEto")]
public  class VerifierCodeEto
{
    public string Type { get; set; }
    public string GuardianAccount { get; set; }
    public Guid VerifierSessionId{ get; set; }
    public string VerifierId{ get; set; }
    public string ChainId { get; set; }
}