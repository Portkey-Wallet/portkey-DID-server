using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class TransakOrderUpdateDto : IThirdPartOrder, IValidatableObject
{
    public string EventId { get; set; }
    public string CreatedAt { get; set; }
    public string WebhookData { get; set; }
    public TransakOrderDto WebhookOrder { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        throw new System.NotImplementedException();
    }
}

public class TransakOrderDto : IThirdPartOrder
{
    public string Id { get; set; }
    public string WalletAddress { get; set; }
    public string CreatedAt { get; set; }
    public string Status { get; set; }
    public string FiatCurrency { get; set; }
    public string UserId { get; set; }
    public string Cryptocurrency { get; set; }
    public string IsBuyOrSell { get; set; }
    public string FiatAmount { get; set; }
    public string CommissionDecimal { get; set; }
    public string FromWalletAddress { get; set; }
    public string WalletLink { get; set; }
    public string AmountPaid { get; set; }
    public string PartnerOrderId { get; set; }
    public string PartnerCustomerId { get; set; }
    public string RedirectUrl { get; set; }
    public string ConversionPrice { get; set; }
    public string CryptoAmount { get; set; }
    public string TotalFee { get; set; }
    public string PaymentOption { get; set; }
    public string AutoExpiresAt { get; set; }
    public string ReferenceCode { get; set; }
    
}