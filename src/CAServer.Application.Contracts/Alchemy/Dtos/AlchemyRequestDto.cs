using System.ComponentModel.DataAnnotations;

namespace CAServer.Alchemy.Dtos;

public class GetAlchemyFreeLoginTokenDto
{
    public string Email { get; set; }
}

public class GetAlchemyCryptoListDto
{
    public string Fiat { get; set; }
}

public class GetAlchemyOrderQuoteDto
{
    [Required]public string Crypto { get; set; }
    [Required]public string Network { get; set; }
    [Required]public string Fiat { get; set; }
    [Required]public string Country { get; set; }
    [Required]public string Amount { get; set; }
    public string PayWayCode { get; set; }
    [Required]public string Side { get; set; }
    public string Type { get; set; }
}