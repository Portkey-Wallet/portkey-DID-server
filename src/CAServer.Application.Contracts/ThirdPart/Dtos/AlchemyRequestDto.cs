using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class GetAlchemyFreeLoginTokenDto
{
    public string Email { get; set; }
}

public class GetAlchemyCryptoListDto
{
    public string Fiat { get; set; }
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
    public string Address { get; set; }
    public object SignParams { get; set; }
}

public class AlchemyOrderUpdateDto : OrderDto, IValidatableObject
{
    [Required] public string MerchantOrderNo { get; set; }
    public string Appid { get; set; }
    public string OrderNo { get; set; }
    public string Email { get; set; }
    
    // card info
    public string Name { get; set; }
    public string Card { get; set; }
    public string Account { get; set; }
    
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
    
    public string CryptoacturalAmount { get; set; }
    public string CryptoActualAmount { get; set; }
    
    public string FailReason { get; set; }
    

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Status) || string.IsNullOrWhiteSpace(Address))
        {
            yield return new ValidationResult(
                $"Invalid input, status :{Status} Address :{Address}"
            );
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

        if (string.IsNullOrWhiteSpace(Signature))
        {
            yield return new ValidationResult(
                $"Invalid order signature :{Signature}."
            );
        }
    }
}

public class UpdateAlchemyTxHashDto
{
    [Required] public string MerchantName { get; set; }
    [Required] public string OrderId { get; set; }
    [Required] public string TxHash { get; set; }
}

public class UpdateAlchemySellOrderDto
{
    public string OrderNo { get; set; }
    public string Address { get; set; }
    public string Crypto { get; set; }
    public string TxHash { get; set; }
    public string CryptoAmount { get; set; }
    public string Network { get; set; }
}