using System.Collections.Generic;
using CAServer.Common;
using CAServer.Commons;

namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions Alchemy { get; set; }
    public ThirdPartTimerOptions Timer { get; set; } = new();
    public MerchantOptions Merchant { get; set; } = new();
    
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
    
    public string NftOrderSettlementPublicKey { get; set; }
    public Dictionary<string, MerchantItem> Merchants { get; set; } = new ();

    public MerchantItem GetOption(string merchantName)
    {
        var getRes = Merchants.TryGetValue(merchantName, out var merchantOption);
        AssertHelper.IsTrue(getRes, "Merchant {Merchant} option not found", merchantName);
        AssertHelper.NotNull(merchantOption, "Merchant {Merchant} option empty", merchantName);
        return merchantOption;
    }
}

public class MerchantItem
{
    public string PublicKey { get; set; }
    public string DidPrivateKey { get; set; }
    public string ReceivingAddress { get; set; }
}
