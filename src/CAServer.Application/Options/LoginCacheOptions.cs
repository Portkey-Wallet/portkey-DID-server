namespace CAServer.Options;

public class LoginCacheOptions
{
    public int RegisterCacheSeconds { get; set; } = 10;

    public int GuardianIdentifiersCacheSeconds { get; set; } = 10;

    public int HolderInfoCacheSeconds { get; set; } = 30;
}