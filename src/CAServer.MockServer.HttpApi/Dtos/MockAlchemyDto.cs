using System;
using System.ComponentModel.DataAnnotations;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using Nest;
using Volo.Abp.EventBus;

namespace MockServer.Dtos;

public class SendTxHashToMockAlchemyDto
{
    [Required] public string OrderNo { get; set; }
    [Required] public string TxHash { get; set; }
}

public class UpdateAlchemyMockOrderDto : AlchemyOrderDto
{
    [Required] public string OrderNo { get; set; }
    [Required] public string Status { get; set; }
}

public class CreateAlchemyMockOrderDto
{
    public string OrderNo { get; set; }
    [Required] public string MerchantOrderNo { get; set; }
    [Required] public string Address { get; set; }
    [Required] public string Crypto { get; set; }
    public string CryptoPrice { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string CryptoQuantity { get; set; } = "";
    public string CryptoActualAmount { get; set; } = "";
}

public class AlchemyResponseDto
{
    public string Success { get; set; } = "Success";
    public string ReturnCode { get; set; } = "0000";
    public string ReturnMsg { get; set; } = "";
    public string Extend { get; set; } = "";
    public string TraceId { get; set; } = "00000000";
}

public class CreateMockAlchemyOrderResponseDto : AlchemyResponseDto
{
    public AlchemyOrderDto Data { get; set; }
}

public class UpdateMockAlchemyOrderResponseDto : AlchemyResponseDto
{
    public AlchemyOrderDto Data { get; set; }
}

[EventName("AlchemyOrderDto")]
public class AlchemyOrderDto
{
    [Keyword] public string OrderNo { get; set; }
    public string Status { get; set; }
    public string Side { get; set; }
    public string Address { get; set; } = "";
    public string PayTime { get; set; } = "";
    public string CompleteTime { get; set; } = "";
    public string MerchantOrderNo { get; set; } = "";
    public string Crypto { get; set; } = "";
    public string Network { get; set; } = "";
    public string CryptoPrice { get; set; } = "";
    public string CryptoAmount { get; set; } = "";
    public string FiatAmount { get; set; } = "";
    public string AppId { get; set; } = "";
    public string Fiat { get; set; } = "";
    public string TxHash { get; set; } = "";
    public string Email { get; set; } = "";
    public string OrderAddress { get; set; } = "";
    public string RampFee { get; set; } = "";
    public string PaymentType { get; set; } = "";
    public string Name { get; set; } = "";
    public string Account { get; set; } = "";
    public string FiatRate { get; set; } = "";
    public string Amount { get; set; } = "";
    public string TxTime { get; set; } = "";
    public string Networkfee { get; set; } = "";
    public string PayType { get; set; } = "";
    public string CryptoQuantity { get; set; } = "";
    public string CryptoActualAmount { get; set; } = "";
    public string Signature { get; set; } = "";
}