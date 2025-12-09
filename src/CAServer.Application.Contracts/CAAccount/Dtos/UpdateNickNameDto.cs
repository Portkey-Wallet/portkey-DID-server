using System.ComponentModel.DataAnnotations;

namespace CAServer.Dtos;

public class UpdateNickNameDto
{
    [Required] [MaxLength(16)] public string NickName { get; set; }
}