using System;
using Orleans;

namespace CAServer.ThirdPart;

[GenerateSerializer]
public class NftOrderGrainDto
{

    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string NftSymbol { get; set; }
    [Id(2)]
    public string NftCollectionName { get; set; }
    [Id(3)]
    public string MerchantName { get; set; }
    [Id(4)]
    public string MerchantOrderId { get; set; }
    [Id(5)]
    public string MerchantAddress { get; set; }
    [Id(6)]
    public string NftPicture { get; set; }
    [Id(7)]
    public DateTime CreateTime { get; set; }
    [Id(8)]
    public DateTime ExpireTime { get; set; }
    [Id(9)]
    public string CaHash { get; set; }
    [Id(10)]
    public string WebhookUrl { get; set; }
    [Id(11)]
    public string WebhookStatus { get; set; } = NftOrderWebhookStatus.NONE.ToString();
    [Id(12)]
    public string WebhookResult { get; set; }
    [Id(13)]
    public string WebhookTime { get; set; }
    [Id(14)]
    public int WebhookCount { get; set; }

    [Id(15)]
    public string ThirdPartNotifyStatus { get; set; } = NftOrderWebhookStatus.NONE.ToString();

    [Id(16)]
    public string ThirdPartNotifyResult { get; set; }

    [Id(17)]
    public string ThirdPartNotifyTime { get; set; }

    [Id(18)]
    public int ThirdPartNotifyCount { get; set; }
}