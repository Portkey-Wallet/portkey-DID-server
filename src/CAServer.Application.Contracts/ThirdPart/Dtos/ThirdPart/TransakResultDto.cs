using System.Collections.Generic;
using CAServer.Commons;
using JetBrains.Annotations;

namespace CAServer.ThirdPart.Dtos.ThirdPart;

public class TransakMetaResponse<TMeta, TData>
{
    [CanBeNull] public TransakError Error { get; set; }
    public TMeta Meta { get; set; }
    public TData Data { get; set; }
    public bool Success => Error == null;
}

public class TransakBaseResponse<TData>
{
    [CanBeNull] public TransakError Error { get; set; }
    public TData Response { get; set; }
    public bool Success => Error == null;
}

public class TransakError
{
    public int StatusCode { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
}

public class TransakCryptoItem
{
    public string CoinId { get; set; }
    public string Address { get; set; }
    public bool AddressAdditionalData { get; set; }
    public string CreatedAt { get; set; }
    public string Decimals { get; set; }
    public bool IsAllowed { get; set; }
    public bool IsPopular { get; set; }
    public bool IsStable { get; set; }
    public string Name { get; set; }
    public int RoundOff { get; set; }
    public string Symbol { get; set; }
    public bool IsIgnorePriceVerification { get; set; }
    public string UniqueId { get; set; }
    public string TokenType { get; set; }
    public string TokenIdentifier { get; set; }
    public bool IsPayInAllowed { get; set; }
    public string MinAmountForPayIn { get; set; }
    public string MaxAmountForPayIn { get; set; }
    public TransakCryptoNetwork Network { get; set; }
    public Dictionary<string, string> Image { get; set; }
}

public class TransakCryptoNetwork
{
    public string Name { get; set; }
    public string ChainId { get; set; }
    public List<string> FiatCurrenciesNotSupported { get; set; }

    public string ToNetworkId()
    {
        return string.Join(CommonConstant.Hyphen, Name.ToUpper(), ChainId.DefaultIfEmpty(CommonConstant.EmptyString));
    }
}

public class TransakFiatItem
{
    public string Symbol { get; set; }
    public List<string> SupportingCountries { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string DefaultCountryForNFT { get; set; }
    public bool IsPopular { get; set; }
    public bool IsAllowed { get; set; }
    public bool IsPayOutAllowed { get; set; }
    public int RoundOff { get; set; }
    public List<TransakFiatPaymentItem> PaymentOptions { get; set; }
}

public class TransakFiatPaymentItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ProcessingTime { get; set; }
    public string Icon { get; set; }
    public bool IsNftAllowed { get; set; }
    public bool IsNonCustodial { get; set; }
    public bool IsActive { get; set; }
    public bool IsPayOutAllowed { get; set; }
    public string LimitCurrency { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? DefaultAmount { get; set; }
    public decimal? DefaultAmountForPayOut { get; set; }
    public decimal? MinAmountForPayOut { get; set; }
    public decimal? MaxAmountForPayOut { get; set; }
    
}

public class TransakRampPrice
{ 
    public string QuoteId { get; set; }
    
    // Fiat : Crypto
    public string ConversionPrice { get; set; }
    public string MarketConversionPrice { get; set; }
    public string Slippage { get; set; }
    public string FiatCurrency { get; set; }
    public string CryptoCurrency { get; set; }
    public string PaymentMethod { get; set; }
    public string FiatAmount { get; set; }
    public string CryptoAmount { get; set; }
    public string IsBuyOrSell { get; set; }
    public string Network { get; set; }
    public string FeeDecimal { get; set; }
    public string TotalFee { get; set; }
    public string Nonce { get; set; }
    public List<TransakRampFee> FeeBreakdown { get; set; }
}

public static class TransakFeeName
{
    public const string TransakFee = "transak_fee";
    public const string NetworkFee = "network_fee";
}

public class TransakRampFee
{
    public string Name { get; set; }
    public decimal Value { get; set; }
    public string Id { get; set; }
    public List<string> Ids { get; set; }
}

public class QueryTransakOrderByIdResult
{
    public TransakOrderDto Data { get; set; }
    
}

public class TransakCountry
{
    public string Alpha2 { get; set; }
    public string Alpha3 { get; set; }
    public string IsAllowed { get; set; }
    public string IsLightKycAllowed { get; set; }
    public string Name { get; set; }
    public string CurrencyCode { get; set; }
    public List<string> SupportedDocuments;
}