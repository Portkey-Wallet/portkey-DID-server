using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.RedPackage.Dtos;

public class SendRedPackageInputDto
{
    [Required] public Guid Id { get; set; }
    [Required] public string TotalAmount { get; set; }
    [Required] public RedPackageType Type { get; set; }
    [Required] public string Memo { get; set; } = string.Empty;
    [Required] public string Symbol { get; set; } = string.Empty;
    [Required] public int Count { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string ChannelUuid { get; set; }
    [Required] public string SendUuid { get; set; }
    [Required] public string RawTransaction { get; set; }
    [Required] public string Message { get; set; }
}