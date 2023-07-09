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
    public const string ResourceTokenKey = "ResourceToken";
    public const int CacheExpirationDays = 365;

    public const string FiatListKey = "FiatList";
    public const string OrderQuoteKey = "OrderQuote";
    public const int FiatListExpirationMinutes = 30;
    public const int OrderQuoteExpirationMinutes = 30;
}