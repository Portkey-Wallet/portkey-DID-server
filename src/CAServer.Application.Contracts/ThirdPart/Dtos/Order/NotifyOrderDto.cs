using System;

namespace CAServer.ThirdPart.Dtos.Order;

public class NotifyOrderDto
{
    public Guid OrderId { get; set; }
    public string MerchantName { get; set; }
    public string Address { get; set; }
    public string Network { get; set; }
    public string Crypto { get; set; }
    public string CryptoAmount { get; set; }
    public string CryptoQuantity { get; set; }
    public string CryptoDecimals { get; set; }
    public string Status { get; set; }
    public string DisplayStatus { get; set; }
    public string TransDirect { get; set; }

    public bool IsNftOrder()
    {
        return TransDirect == TransferDirectionType.NFTBuy.ToString() ||
               TransDirect == TransferDirectionType.NFTSell.ToString();
    }

}