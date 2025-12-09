namespace CAServer.Commons;

public class HttpConstant
{
    public const long RetryDelayMs = 500;
    public const int RetryCount = 3;
    public const string RetryHttpClient = "WaitAndRetryClient";
}