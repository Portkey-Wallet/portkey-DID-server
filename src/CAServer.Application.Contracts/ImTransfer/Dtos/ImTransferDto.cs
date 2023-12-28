using System.ComponentModel.DataAnnotations;
using CAServer.EnumType;

namespace CAServer.ImTransfer.Dtos;

public class ImTransferDto
{
    public GroupType Type { get; set; }
    [Required] public string ToUserId { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string ChannelUuid { get; set; }
    [Required] public string RawTransaction { get; set; }
    [Required] public string Message { get; set; }
}