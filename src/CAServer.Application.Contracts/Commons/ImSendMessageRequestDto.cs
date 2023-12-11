namespace CAServer.Commons;

public class ImSendMessageRequestDto
{
    public string ToRelationId { get; set; }

    public string ChannelUuid { get; set; }

    public string Type { get; set; }

    public string Content { get; set; }

    public string SendUuid { get; set; }

    public string QuoteId { get; set; }

    public string[] MentionedUser { get; set; }
}