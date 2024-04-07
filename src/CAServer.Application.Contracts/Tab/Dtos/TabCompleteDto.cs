using System.ComponentModel.DataAnnotations;

namespace CAServer.Tab.Dtos;

public class TabCompleteDto
{
    [Required] public string ClientId { get; set; }
    [Required] public string MethodName { get; set; }
    public string Data { get; set; }
}