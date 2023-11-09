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
    
    public List<OrderDto> OrderList { get; set; }

    public OrderExportResponseDto(List<OrderDto> orderList)
    {
        OrderList = orderList;
    }

    public string ToJsonText()
    {
        var jsonBuilder = new StringBuilder();
        var stringWriter = new StringWriter(jsonBuilder);
        stringWriter.WriteLine("[");
        for (var i = 0 ; i < OrderList.Count ; i ++)
        {
            var order = OrderList[i];
            stringWriter.Write(JsonConvert.SerializeObject(order));
            if (i < OrderList.Count - 1)
                stringWriter.Write(",");
            stringWriter.WriteLine();
        }
        
        stringWriter.WriteLine("]");
        stringWriter.Flush();
        return jsonBuilder.ToString();
    }
    
    
    public string ToCsvText()
    {
        var csvBuilder = new StringBuilder();
        var stringWriter = new StringWriter(csvBuilder);
        stringWriter.WriteLine(string.Join(CommonConstant.Comma, 
            "OrderId",
            "UserId",
            "ThirdPartOrderId",
            "ThirdPartName",
            "Type",
            "Address",
            "Status",
            "Crypto",
            "CryptoAmount",
            "Fiat",
            "FiatAmount",
            "TransactionId",
            "NFTSymbol",
            "NFTCollectionName",
            "MerchantName",
            "CreateTime",
            "LastModifyTime"
        ));
        
        foreach (var order in OrderList)
        {
            stringWriter.WriteLine(string.Join(CommonConstant.Comma, 
                order.Id.ToString(),
                order.UserId.ToString(),
                order.ThirdPartOrderNo,
                order.MerchantName,
                order.TransDirect,
                order.Address,
                order.Status,
                order.Crypto,
                order.CryptoAmount,
                order.Fiat,
                order.FiatAmount,
                order.TransactionId,
                order.NftOrderSection?.NftSymbol ?? CommonConstant.EmptyString,
                order.NftOrderSection?.NftCollectionName ?? CommonConstant.EmptyString,
                order.NftOrderSection?.MerchantName ?? CommonConstant.EmptyString,
                TimeHelper.GetDateTimeFromTimeStamp(order.NftOrderSection?.CreateTime ?? 0).ToUtcString(),
                TimeHelper.GetDateTimeFromTimeStamp(order.LastModifyTime.SafeToLong()).ToUtcString()
            ));   
        }
        
        stringWriter.Flush();
        return csvBuilder.ToString();
    }
}