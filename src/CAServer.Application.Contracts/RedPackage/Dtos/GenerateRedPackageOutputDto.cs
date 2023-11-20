using System;
using Org.BouncyCastle.Asn1.Crmf;

namespace CAServer.RedPackage.Dtos;

public class GenerateRedPackageOutputDto
{
    public Guid Id { get; set; }
    public string PublicKey { get; set; }
    public string Signature { get; set; }
    public decimal MinAmount { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string ChainId { get; set; }
}