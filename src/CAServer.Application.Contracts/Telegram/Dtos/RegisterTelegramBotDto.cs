using System.ComponentModel.DataAnnotations;

namespace CAServer.Telegram.Dtos;

public class RegisterTelegramBotDto
{
    [Required]
    public string Secret { get; set; }
    
}