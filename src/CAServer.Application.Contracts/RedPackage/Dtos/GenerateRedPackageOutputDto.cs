using System;
using CAServer.Commons.Etos;
using Org.BouncyCastle.Asn1.Crmf;

namespace CAServer.RedPackage.Dtos;

public class GenerateRedPackageOutputDto : ChainDisplayNameDto
{
    public Guid Id { get; set; }
    public string PublicKey { get; set; }
    public string MinAmount { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public long ExpireTime { get; set; }
    public string RedPackageContractAddress { get; set; }
}