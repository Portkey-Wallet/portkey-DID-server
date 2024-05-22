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
    

    public const string GetConnectToken = "connect/token";
    public const string GetTokenList = "token/list";
    public const string GetTokenOptionList = "token/option";
    public const string GetNetworkList = "network/list";
    public const string CalculateDepositRate = "deposit/calculator";
    public const string GetDepositInfo = "deposit/info";
    public const string GetOrderRecordList = "record/list";
}