using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CAServer.ThirdPart.Dtos.ThirdPart;

public class TransakEventRawDataDto : IThirdPartOrder, IValidatableObject
{
    public string Data { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Data))
        {
            yield return new ValidationResult("Invalid input");
        }
    }
}

public class TransakOrderUpdateEventDto : IThirdPartOrder, IValidatableObject
{
    public string EventId { get; set; }
    public string CreatedAt { get; set; }
    private string _webhookData;
    public string WebhookData
    {
        get => _webhookData;
        set
        {
            _webhookData = value;
            WebhookOrder = string.IsNullOrWhiteSpace(_webhookData)
                ? null
                : JsonConvert.DeserializeObject<TransakOrderDto>(_webhookData, new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
    public TransakOrderDto WebhookOrder { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(EventId) || string.IsNullOrWhiteSpace(CreatedAt) ||
            string.IsNullOrWhiteSpace(WebhookData))
        {
            yield return new ValidationResult("Invalid input");
        }
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

    public bool IsBuy()
    {
        return IsBuyOrSell == OrderTransDirect.BUY.ToString();
    }
    
    public bool IsSell()
    {
        return IsBuyOrSell == OrderTransDirect.SELL.ToString();
    }
}

public class GetRampPriceRequest
{
    public string PartnerApiKey { get; set; }
    public string FiatCurrency { get; set; }
    public string CryptoCurrency { get; set; }
    public string IsBuyOrSell { get; set; }
    public string Network { get; set; }
    public string PaymentMethod { get; set; }
    public string FiatAmount { get; set; }
    public string CryptoAmount { get; set; }
}

public class UpdateWebhookRequest
{
    public string WebhookURL { get; set; }
}

public class TransakAccessTokenResp
{
    public TransakAccessToken Data { get; set; }
}

public class TransakAccessToken
{
    public string AccessToken { get; set; }
    public long ExpiresAt { get; set; }
}