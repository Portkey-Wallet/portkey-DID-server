using CAServer.ThirdPart.Dtos;

namespace CAServer.Grains.State.Order;

public class NftOrderState
{
    public Guid Id { get; set; }
    public string NftSymbol { get; set; }
    public string MerchantName { get; set; }
    public string MerchantOrderId { get; set; }
    public string NftPicture { get; set; }
    public string WebhookUrl { get; set; }
    public string WebhookStatus { get; set; } = NftOrderWebhookStatus.NONE.ToString();
    public int WebhookCount { get; set; } = 0;
    public string WebhookResult { get; set; }
    public string WebhookTime { get; set; }
}