using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.EnumType;
using Newtonsoft.Json;
using Orleans;

namespace CAServer.RedPackage.Dtos;

[GenerateSerializer]
public class SendRedPackageInputDto
{
    [Id(0)]
    public RedPackageDisplayType RedPackageDisplayType { get; set; }
    [Id(1)]
    public bool IsNewUsersOnly { get; set; }
    [Id(2)]
    [Required] public Guid Id { get; set; }
    [Id(3)]
    [Required] public string TotalAmount { get; set; }
    [Id(4)]
    [Required] public RedPackageType Type { get; set; }
    [Id(5)]
    [Required] public string Memo { get; set; } = string.Empty;
    [Id(6)]
    [Required] public string Symbol { get; set; } = string.Empty;
    [Id(7)]
    [Required] public int Count { get; set; }
    [Id(8)]
    [Required] public string ChainId { get; set; }
    // [Required]
    [Id(9)]
    public string ChannelUuid { get; set; }
    [Id(10)]
    [Required] public string RawTransaction { get; set; }
    // [Required] im scene required, crypto gift not required
    [Id(11)]
    public string Message { get; set; }
    [Id(12)]
    public int AssetType { get; set; }
    [Id(13)]
    public Guid SessionId { get; set; }
}