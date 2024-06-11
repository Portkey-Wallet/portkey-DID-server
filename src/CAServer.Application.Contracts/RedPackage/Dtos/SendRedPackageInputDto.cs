using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.EnumType;
using Newtonsoft.Json;

namespace CAServer.RedPackage.Dtos;

public class SendRedPackageInputDto
{
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
    public bool IsNewUsersOnly { get; set; }
    [Required] public Guid Id { get; set; }
    [Required] public string TotalAmount { get; set; }
    [Required] public RedPackageType Type { get; set; }
    [Required] public string Memo { get; set; } = string.Empty;
    [Required] public string Symbol { get; set; } = string.Empty;
    [Required] public int Count { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string ChannelUuid { get; set; }
    [Required] public string RawTransaction { get; set; }
    // [Required] im scene required, crypto gift not required
    public string Message { get; set; }
    public int AssetType { get; set; }
}