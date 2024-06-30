using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CryptoGift.Dtos;

public class CryptoGiftStatisticsRequestDto
{
    [Required] 
    public bool NewUsersOnly { get; set; }
    [Required] 
    public List<string> Symbols { get; set; }
    [Required]
    public long CreateTime { get; set; }
}