using System.ComponentModel.DataAnnotations;

namespace CAServer.Dtos;

public class UpdateNickNameDto
{
    [Required] [Range(1, 16)] public string NickName { get; set; }
}