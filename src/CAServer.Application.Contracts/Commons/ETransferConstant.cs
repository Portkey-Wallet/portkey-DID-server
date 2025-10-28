namespace CAServer.Commons;

public static class ETransferConstant
{
    public const string ClientName = "ETransfer";
    public const string AuthHeader = "T-Authorization";
    public const string ClientId = "ETransferServer_App";
    public const string GrantType = "signature";
    public const string Version = "v2";
    public const string Source = "portkey";
    public const string Scope = "ETransferServer";
    public const string SuccessCode = "20000";
    public const int DefaultSkipCount = 0;
    public const int DefaultMaxResultCount = 1000;
    public const string SgrName = "SGR-1";
    public const string SgrDisplayName = "SGR";
    public const string DepositName = "Deposit";
    public const int DefaultConfirmBlock = 64;
    public const string Confirmation = "confirmation";
    public const string Network = "network";
    public const string ToType = "to";
    public const string TronName = "TRX";
    public const string DefaultToken = "ELF";
    public const string InvalidAddressCode = "40001";
    public const string InvalidAddressMessage= "Invalid address";
    

    public const string GetConnectToken = "connect/token";
    public const string GetTokenList = "token/list";
    public const string GetTokenOptionList = "token/option";
    public const string GetNetworkList = "network/list";
    public const string CalculateDepositRate = "deposit/calculator";
    public const string GetDepositInfo = "deposit/info";
    public const string GetOrderRecordList = "record/list";
}