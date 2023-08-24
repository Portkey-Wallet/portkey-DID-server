using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Security.Dtos;

public class GetUserSecuritySelfTestDto
{
    // UserId Only available in test sessions since we don't' get authorized user. 
    public Guid UserId { get; set; }
    [Required] public string CaHash { get; set; }
}