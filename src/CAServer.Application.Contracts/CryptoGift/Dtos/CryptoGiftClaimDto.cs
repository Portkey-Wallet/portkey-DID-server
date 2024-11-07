using System;
using CAServer.Commons.Etos;

namespace CAServer.CryptoGift.Dtos;

public class CryptoGiftClaimDto : ChainDisplayNameDto
{
    public Guid UserId { get; set; }
    
    public string CaAddress { get; set; }
    
    public int Number { get; set; }
    
    public int Count { get; set; }
    
    public int Grabbed { get; set; }
}