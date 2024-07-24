using System.ComponentModel.DataAnnotations;

namespace CAServer.FreeMint.Dtos;

public class ConfirmRequestDto
{
    [Required] public string ImageUrl { get; set; }
    [Required] public string Name { get; set; }
    public string Description { get; set; }
}