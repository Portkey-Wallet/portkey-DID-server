using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart.Dtos;

public class GetAlchemyFreeLoginTokenDto
{
    public string Email { get; set; }
}

public class GetAlchemyCryptoListDto
{
    public string Fiat { get; set; }
}

public class GetAlchemyFiatListDto
{
    public string Type { get; set; } = "BUY";
}

public class GetAlchemyOrderQuoteDto
{
    [Required] public string Crypto { get; set; }
    [Required] public string Network { get; set; }
    [Required] public string Fiat { get; set; }
    [Required] public string Country { get; set; }
    [Required] public string Amount { get; set; }
    [Required] public string Side { get; set; }
    public string PayWayCode { get; set; }
    public string Type { get; set; }
}

public class GetAlchemySignatureDto
{
    [Required] public string Address { get; set; }
}

public class AlchemyOrderUpdateDto : OrderDto, IValidatableObject
{
    [Required] public string MerchantOrderNo { get; set; }
    public string OrderNo { get; set; }
    public string PayType { get; set; }
    public string Amount { get; set; }
    public string PayTime { get; set; }
    public string TxHash { get; set; }
    public string Message { get; set; }
    public string Signature { get; set; }
    public string NetworkFee { get; set; }
    public string RampFee { get; set; }
    public string CompleteTime { get; set; }
    public string OrderAddress { get; set; }
    public string PaymentType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Status) || string.IsNullOrWhiteSpace(Address) ||
            string.IsNullOrWhiteSpace(Signature) || string.IsNullOrWhiteSpace(OrderNo) ||
            string.IsNullOrWhiteSpace(Crypto))
        {
            yield return new ValidationResult("Invalid input");
        }

        if (!ThirdPartHelper.ValidateMerchantOrderNo(MerchantOrderNo))
        {
            yield return new ValidationResult(
                $"Invalid third part order number :{MerchantOrderNo}."
            );
        }

        if (!AlchemyHelper.OrderStatusExist(Status))
        {
            yield return new ValidationResult(
                $"Invalid order status :{Status}."
            );
        }
    }
}

public class SendAlchemyTxHashDto
{
    public string MerchantName { get; set; }
    [Required] public string OrderId { get; set; }
    [Required] public string TxHash { get; set; }
}

public class WaitToSendOrderInfoDto
{
    public string OrderNo { get; set; }
    public string Crypto { get; set; }
    public string CryptoAmount { get; set; }

    public string TxHash { get; set; }
    public string Network { get; set; }
    public string Address { get; set; }
    public string AppId { get; set; }
    public string Signature { get; set; }
}

public class QueryAlchemyOrderInfoDto
{
    public string OrderId { get; set; }
}

public class OrderQueryDto
{
    public string OrderNo { get; set; }
    public string MerchantOrderNo { get; set; }
    public string Side { get; set; }
}

public class QueryAlchemyOrderInfo
{
    public string OrderNo { get; set; }
    public string Address { get; set; }
    public string PayTime { get; set; }
    public string CompleteTime { get; set; }
    public string MerchantOrderNo { get; set; }
    public string Crypto { get; set; }
    public string Network { get; set; }
    public string CryptoPrice { get; set; }
    public string CryptoAmount { get; set; }
    public string FiatAmount { get; set; }
    public string AppId { get; set; }
    public string Fiat { get; set; }
    public string TxHash { get; set; }
    public string Email { get; set; }
    public string OrderAddress { get; set; }
    public string CryptoActualAmount { get; set; }
    public string RampFee { get; set; }
    public string PaymentType { get; set; }
    public string Name { get; set; }
    public string Account { get; set; }
    public string FiatRate { get; set; }
    public string Status { get; set; }
    public string Side { get; set; }
    public string Amount { get; set; }
    public string TxTime { get; set; }
    public string Networkfee { get; set; }
    public string PayType { get; set; }
    public string CryptoQuantity { get; set; }
}


public class AlchemyNftOrderDto : IThirdPartValidOrderUpdateRequest
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    
    public string Amount { get; set; }
    public string Fiat { get; set; }
    public string OrderNo { get; set; }
    public string PayTime { get; set; }
    public string PayType { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public string Quantity { get; set; }
    public string UniqueId { get; set; }
    public string AppId { get; set; }
    public string MerchantOrderNo { get; set; }
    public string Message { get; set; }
}

public class AlchemyNftOrderRequestDto : AlchemyNftOrderDto, IThirdPartNftOrderUpdateRequest
{
    public string Signature { get; set; }
}

public class AlchemyNftReleaseNoticeRequestDto
{
    public string MerchantOrderNo { get; set; }
    public string OrderNo { get; set; }
    public string ReleaseStatus { get; set; }
    public string TransactionHash { get; set; }
    public string ReleaseTime { get; set; }
    public string Contract { get; set; }
    public string UniqueId { get; set; }
    public string Picture { get; set; }
    public string PictureNumber { get; set; }
}