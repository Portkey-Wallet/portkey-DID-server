namespace CAServer.Options;

public class ContactMigrateOptions
{
    public bool Open { get; set; } = true;
    public int Period { get; set; } = 200;
    public int MigrateCount { get; set; } = 1000;
    public string UserId { get; set; }
}