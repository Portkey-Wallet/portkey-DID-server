using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.RedPackage.Dtos;

public class GrabRedPackageInputDto
{
    [Required] public Guid Id { get; set; }
    [Required] public string UserCaAddress { get; set; }
}