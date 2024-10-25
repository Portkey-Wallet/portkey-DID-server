using System;

namespace CAServer.Commons;

public static class AElfContractMethodName
{
    public const string GetHolderInfo = "GetHolderInfo";
    public const string GetVerifierServers = "GetVerifierServers";
    public const string GetBalance = "GetBalance";
    public const string ClaimToken = "ClaimToken";
    public const string Transfer = "Transfer";
}

public static class CommonConstant
{
    public const string EmptyString = "";
    public const string Dot = ".";
    public const string Hyphen = "-";
    public const string Colon = ":";
    public const string Underline = "_";
    public const string Comma = ",";

    public const string CurrencyFiat = "Fiat";
    public const string CurrencyCrypto = "Crypto";
    
    public const string ResourceTokenKey = "ResourceToken";
    public const int CacheExpirationDays = 365;
    public const string CacheCorrectUserTokenBalancePre = "CorrectUserTokenBalance:{0}:{1}:{2}";
    public const long CacheTokenBalanceExpirationSeconds = 60;

    public const string FiatListKey = "FiatList";
    public const string NftFiatListKey = "NftFiatList";
    public const int FiatListExpirationMinutes = 30;
    public const int CryptoListExpirationMinutes = 30;
    public const int OrderQuoteExpirationMinutes = 30;
    public static DateTimeOffset DefaultAbsoluteExpiration = DateTime.Parse("2099-01-01 12:00:00");
    public const string OrderStatusInfoPrefix = "OrderStatusInfo";

    public const string ChainName = "aelf";
    public const string ELF = "ELF";
    public const string USD = "USD";
    public const string USDT = "USDT";

    public const string MainChainId = "AELF";
    public const string TDVVChainId = "tDVV";
    public const string TDVWChainId = "tDVW";
    public const double DefaultAchFee = 0.39;
    public const double DefaultCrossChainFee = 0.35;
    public const double DefaultMaxFee = 0.39;

    public const string AppleTransferMessage =
        "We are currently upgrading our system to serve you better. During this period, the Apple ID service is temporarily unavailable.";

    public const string AuthHeader = "Authorization";
    public const string ImAuthHeader = "R-Authorization";
    public const string SuccessCode = "20000";

    public const string AppleRevokeUrl = "https://appleid.apple.com/auth/revoke";
    public const string ImFollowUrl = "api/v1/contacts/follow";
    public const string ImUnFollowUrl = "api/v1/contacts/unfollow";
    
    public const string UserExtraInfoIdPrefix = "UserExtraInfo-";
    
    public const string ApplicationName = "Portkey";
    public const string ModuleName = "Api";
    
    public const int WalletNameDefaultLength = 8;
    public const string AppVersionKeyPrefix = "AppVersion";
    public const string DefaultSymbol = "ELF";
    public const string TransferCard = "transfer-card";
    
    public const string DefaultFiatUSD = "USD";
    public const string DefaultCryptoELF = "ELF";
    public const string DefaultFiatPrice = "200";
    public const string DefaultCryptoPrice = "400";

    public const string ProtocolName = "http";
    public const string UpgradeGrainIdPrefix = "UpgradeInfo";
    
    public const string SyncStateUri = "apps/sync-state";
    public const string ReplaceUri = "app/graphql";

}