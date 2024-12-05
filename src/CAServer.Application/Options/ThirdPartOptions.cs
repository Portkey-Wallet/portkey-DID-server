using System.Collections.Generic;
using CAServer.Common;
using CAServer.Commons;

namespace CAServer.Options;

public class ThirdPartOptions
{
    public AlchemyOptions Alchemy { get; set; }
    public TransakOptions Transak { get; set; }
    public ThirdPartTimerOptions Timer { get; set; } = new();
    public MerchantOptions Merchant { get; set; } = new();
    public OrderExportAuth OrderExportAuth { get; set; }
    public TreasuryOptions TreasuryOptions { get; set; }

}

public class TreasuryOptions
{
    // ThirdPartName_Crypto => settlementPublicKey
    public Dictionary<string, string> SettlementPublicKey { get; set; } = new();

    public int TransferRetryMaxCount = 5;
    
    public decimal ValidAmountPercent = 0.01M;
}

public class OrderExportAuth
{
    public string Key { get; set; }
    public string UserName { get; set; }
    public string AccountTitle { get; set; }
}

public class ThirdPartTimerOptions
{
    public int DelaySeconds { get; set; } = 1;
    public int TimeoutMillis { get; set; } = 60000;
    public int TransactionWaitDelaySeconds { get; set; } = 2;
    public int TransactionWaitTimeoutSeconds { get; set; } = 45;
    public int TransactionConfirmHeight { get; set; } = 100;
    public int HandleUnCompletedOrderMinuteAgo { get; set; } = 2;
    public int HandleUnCompletedOrderPageSize { get; set; } = 10;
    public int HandleUnCompletedSettlementTransferSecondsAgo { get; set; } = 30;
    public int HandleUnCompletedSettlementTransferPageSize { get; set; } = 10;
    public int HandleUnCompletedSettlementTransferMinuteAgo { get; set; } = 15;
    public int NftCheckoutMerchantCallbackCount { get; set; }  = 3;
    public int NftCheckoutMerchantCallbackPageSize { get; set; }  = 10;
    public int NftCheckoutResultThirdPartNotifyCount { get; set; }  = 3;
    public int NftCheckoutResultThirdPartPageSize { get; set; }  = 10;
    public int NftUnCompletedMerchantCallbackMinuteAgo { get; set; }  = 2;
    public int NftUnCompletedThirdPartCallbackMinuteAgo { get; set; }  = 2;
    
    // Handle un complete order settlement from days ago to minutes ago
    public int NftUnCompletedOrderSettlementMinuteAgo { get; set; } = 2;
    public int NftUnCompletedOrderSettlementDaysAgo { get; set; }  = 2;
    public int NftUnCompletedOrderSettlementPageSize { get; set; }  = 10;
    
    public int RampUnCompletedSettlementMinuteAgo { get; set; }  = 2;
    public int NftOrderExpireSeconds { get; set; } = 60 * 30;
    public int TreasuryTxConfirmWorkerPageSize { get; set; } = 10;
    public int TreasuryCallbackFromMinutesAgo { get; set; } = 60;
    public int TreasuryCallbackMaxCount { get; set; } = 3;
    public int PendingTreasuryOrderExpireSeconds { get; set; } = 1800;

}

public class AlchemyOptions
{
    public string AppId { get; set; }
    public string BaseUrl { get; set; }
    public string NftAppId { get; set; }
    public string NftBaseUrl { get; set; }
    public string UpdateSellOrderUri { get; set; }
    public string FiatListUri { get; set; }
    public string CryptoListUri { get; set; }
    public string OrderQuoteUri { get; set; }
    public string GetTokenUri { get; set; }
    public bool SkipCheckSign { get; set; } = false;
    public int FiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int CryptoListExpirationMinutes { get; set; } = CommonConstant.CryptoListExpirationMinutes;
    public int NftFiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int OrderQuoteExpirationMinutes { get; set; } = CommonConstant.OrderQuoteExpirationMinutes;
    public string MerchantQueryTradeUri { get; set; }
    public int TimestampExpireSeconds { get; set; } = 300;
    public decimal EffectivePricePercentage { get; set; }  = 0.1M;
}

public class TransakOptions
{
    public string AppId { get; set; }
    public string BaseUrl { get; set; }
    public double RefreshTokenDurationPercent { get; set; } = 0.8;
    public int FiatListExpirationMinutes { get; set; } = CommonConstant.FiatListExpirationMinutes;
    public int CryptoListExpirationMinutes { get; set; } = CommonConstant.CryptoListExpirationMinutes;
    public int OrderQuoteExpirationMinutes { get; set; } = CommonConstant.OrderQuoteExpirationMinutes;
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
    public string ReceivingAddress { get; set; }
}
