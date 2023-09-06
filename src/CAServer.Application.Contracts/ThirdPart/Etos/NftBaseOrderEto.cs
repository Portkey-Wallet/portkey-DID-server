using System;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("NFTOrderEto")]
public class NftOrderEto
{

    public string Id { get; set; }
    public string NftSymbol { get; set; }
    public string MerchantName { get; set; }
    public string MerchantOrderId { get; set; }
    public string NftPicture { get; set; }
    public string WebhookUrl { get; set; }
    public string WebhookResult { get; set; }
    public string WebhookTime { get; set; }
    public string WebhookStatus { get; set; }
    public int WebhookCount { get; set; } = 0;
    public string ThirdPartNotifyStatus { get; set; }
    public string ThirdPartNotifyResult { get; set; }
    public string ThirdPartNotifyTime { get; set; }
    public int ThirdPartNotifyCount { get; set; } = 0;
}