using System.ComponentModel.DataAnnotations;

namespace CAServer.Dtos;

public class UpdateNickNameDto
{
    [Required] public string NickName { get; set; }
}