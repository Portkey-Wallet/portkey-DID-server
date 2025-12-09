using System;
using System.ComponentModel.DataAnnotations;
using CAServer.EnumType;

namespace CAServer.RedPackage.Dtos;

public class GrabRedPackageInputDto
{
    [Required] public Guid Id { get; set; }
    [Required] public string UserCaAddress { get; set; }
    public string CaHash { get; set; }
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
    
    public string Random { get; set; }
}