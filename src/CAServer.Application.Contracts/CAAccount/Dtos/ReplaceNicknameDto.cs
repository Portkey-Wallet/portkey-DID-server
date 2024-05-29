using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class ReplaceNicknameDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string CaHash { get; set; }
    [Required] public bool ReplaceNickname { get; set; }
}