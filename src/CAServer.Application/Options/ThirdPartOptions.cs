using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions Alchemy { get; set; }
    public ThirdPartTimerOptions Timer { get; set; } = new();
    public MerchantOptions Merchant { get; set; } = new();
    
    public string NftOrderSettlementPublicKey { get; set; }
}

public class ThirdPartTimerOptions
{
    public int DelaySeconds { get; set; } = 1;
    public int TimeoutMillis { get; set; } = 60000;
    public int TransactionWaitDelaySeconds { get; set; } = 5;
    public int TransactionWaitTimeoutSeconds { get; set; } = 45;
    public int HandleUnCompletedOrderMinuteAgo { get; set; } = 2;
    public int HandleUnCompletedSettlementTransferSecondsAgo { get; set; } = 60;
    public int NftCheckoutMerchantCallbackCount { get; set; }  = 3;
    public int NftCheckoutResultThirdPartNotifyCount { get; set; }  = 3;
    public int NftUnCompletedMerchantCallbackMinuteAgo { get; set; }  = 2;
    public int NftUnCompletedThirdPartCallbackMinuteAgo { get; set; }  = 2;
    public int NftOrderExpireSeconds { get; set; }  = 60 * 30;
}

public class AlchemyOptions
{
    public string AppId { get; set; }
    public string AppSecret { get; set; }
    public string BaseUrl { get; set; }
    
    public string NftAppId { get; set; }
    public string NftAppSecret { get; set; }
    public string NftBaseUrl { get; set; }
    public string UpdateSellOrderUri { get; set; }
    public string FiatListUri { get; set; }
    public string CryptoListUri { get; set; }
    public string OrderQuoteUri { get; set; }
    public string GetTokenUri { get; set; }
    public bool SkipCheckSign { get; set; } = false;
    public int FiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int NftFiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int OrderQuoteExpirationMinutes { get; set; } = CommonConstant.OrderQuoteExpirationMinutes;
    public string MerchantQueryTradeUri { get; set; }
}

public class MerchantOptions
{
    
    public string EncryptionKey { get; set; }
    
    // merchantName => publicKey, publicKey of merchant
    public Dictionary<string, string> MerchantPublicKey { get; set; } = new();
    
    // merchantName => privateKey, privateKey of Did, diff pk for diff merchant 
    public Dictionary<string, string> DidPrivateKey { get; set; } = new();
}
