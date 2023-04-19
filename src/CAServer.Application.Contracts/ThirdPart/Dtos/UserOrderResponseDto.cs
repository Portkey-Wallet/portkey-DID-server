using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class BasicOrderResult
{
    public bool Success { get; set; } = false;
    public string Message { get; set; }
}

public class OrderCreatedDto : BasicOrderResult
{
    public string Id { get; set; }
}

public class OrdersDto
{
    public List<OrderDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class OrderDto
{
    // common parameters
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MerchantName { get; set; }
    public string TransDirect { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string CryptoPrice { get; set; }
    public string Fiat { get; set; }
    public string FiatAmount { get; set; }
    public string LastModifyTime { get; set; }
    public string Status { get; set; }

    // buy order
    public string CryptoQuantity { get; set; }
    public string PaymentMethod { get; set; }
    public string TxTime { get; set; }

    // sell order
    public string ReceivingMethod { get; set; }
    public string ReceiptTime { get; set; }
}