using System.ComponentModel.DataAnnotations;

namespace CAServer.Demo.Dtos;

public class DemoRequestDto
{
    [Required] public string Name { get; set; }

    [Range(10, 20)] public int Age { get; set; }
}