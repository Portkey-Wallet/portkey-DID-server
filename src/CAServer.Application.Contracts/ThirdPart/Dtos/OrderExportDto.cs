using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CAServer.Commons;
using Newtonsoft.Json;

namespace CAServer.ThirdPart.Dtos;

public class OrderExportRequestDto
{
    public string Type { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public List<string> Status { get; set; }
    public string ThirdPart { get; set; }
    public string ReturnType { get; set; } = "csv";
    public string Auth { get; set; }
}

public class OrderExportResponseDto
{
    private const string Empty = CommonConstant.EmptyString;
    
    
    public List<OrderDto> OrderList { get; set; }

    public OrderExportResponseDto(List<OrderDto> orderList)
    {
        OrderList = orderList;
    }
    
    public string ToCsvText()
    {
        var csvBuilder = new StringBuilder();
        var stringWriter = new StringWriter(csvBuilder);
        stringWriter.WriteLine(string.Join(CommonConstant.Comma,
            "PortkeyOrderId",
            "PortkeyUserId",
            "Type",
            "Status",

            // settlement
            "Binance_Crypto_USDT",
            "BinanceSettlement_USDT",
            "Okx_Crypto_USDT",
            "OkxSettlement_USDT",
            
            // currencies
            "Crypto",
            "CryptoAmount",
            "Fiat",
            "FiatAmount",
            
            // thirdPart
            "ThirdPartOrderId",
            "ThirdPartName",
            "ThirdPartCallBackTime",
            
            // addresses
            "UserAddress",
            "ThirdPartAddress",
            "MerchantAddress",
            
            // transaction
            "ReceiveCryptoTxId",
            "SendCryptoTxId",
            "SettlementTxId",
            
            // NFT
            "NFTSymbol",
            "NFTCollectionName",
            
            // merchant
            "MerchantName",
            "MerchantOrderId",
            "MerchantCallBackTime",
            
            // times
            "CreateTime",
            "PaySuccessTime",
            "FinishTime",
            "LastModifyTime"
        ));

        foreach (var order in OrderList)
        {
            var paySuccessTime = order.OrderStatusSection?.StateTime(OrderStatusType.Pending) ?? 0;
            var finishTime = order.OrderStatusSection?.StateTime(OrderStatusType.Finish) ?? 0;

            stringWriter.WriteLine(string.Join(CommonConstant.Comma,
                order.Id.ToString(),
                order.UserId.ToString(),
                order.TransDirect,
                order.Status,

                // settlement
                order.OrderSettlementSection?.BinanceExchange ?? Empty,
                order.OrderSettlementSection?.BinanceSettlementAmount ?? Empty,
                order.OrderSettlementSection?.OkxExchange ?? Empty,
                order.OrderSettlementSection?.OkxSettlementAmount ?? Empty,

                // currencies
                order.Crypto,
                order.CryptoAmount,
                order.Fiat,
                order.FiatAmount,

                // thirdPart
                order.ThirdPartOrderNo,
                order.MerchantName,
                order.NftOrderSection?.ThirdPartNotifyStatus == NftOrderWebhookStatus.SUCCESS.ToString()
                && (order.NftOrderSection?.ThirdPartNotifyTime?.NotNullOrEmpty() ?? false)
                    ? TimeHelper.ParseFromUtcString(order.NftOrderSection?.ThirdPartNotifyTime).ToUtc8String()
                    : Empty,

                // addresses
                order.TransDirect == TransferDirectionType.TokenBuy.ToString() ||
                order.TransDirect == TransferDirectionType.NFTBuy.ToString()
                    ? order.Address
                    : Empty,
                order.TransDirect == TransferDirectionType.TokenSell.ToString() ? order.Address : Empty,
                order.NftOrderSection?.MerchantAddress ?? Empty,

                // transaction
                order.TransDirect == TransferDirectionType.TokenBuy.ToString() ? order.TransactionId : Empty,
                order.TransDirect == TransferDirectionType.TokenSell.ToString() ? order.TransactionId : Empty,
                order.TransDirect == TransferDirectionType.NFTBuy.ToString() ? order.TransactionId : Empty,

                // NFT
                order.NftOrderSection?.NftSymbol ?? Empty,
                order.NftOrderSection?.NftCollectionName ?? Empty,

                // merchant
                order.NftOrderSection?.MerchantName ?? Empty,
                order.NftOrderSection?.MerchantOrderId ?? Empty,
                order.NftOrderSection?.WebhookStatus == NftOrderWebhookStatus.SUCCESS.ToString()
                && (order.NftOrderSection?.WebhookTime.NotNullOrEmpty() ?? false)
                    ? TimeHelper.ParseFromUtcString(order.NftOrderSection?.WebhookTime).ToUtc8String()
                    : Empty,

                // times
                (order.NftOrderSection?.CreateTime ?? 0) > 0
                    ? TimeHelper.GetDateTimeFromTimeStamp(order.NftOrderSection?.CreateTime ?? 0).ToUtc8String() 
                    : Empty,
                paySuccessTime > 0
                    ? TimeHelper.GetDateTimeFromTimeStamp(paySuccessTime).ToUtc8String()
                    : Empty,
                finishTime > 0
                    ? TimeHelper.GetDateTimeFromTimeStamp(finishTime).ToUtc8String()
                    : Empty,
                order.LastModifyTime.NotNullOrEmpty()
                    ? TimeHelper.GetDateTimeFromTimeStamp(order.LastModifyTime.SafeToLong()).ToUtc8String() 
                    : Empty
            ));
        }

        stringWriter.Flush();
        return csvBuilder.ToString();
    }
}