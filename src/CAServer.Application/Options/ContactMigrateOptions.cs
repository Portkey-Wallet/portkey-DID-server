namespace CAServer.Options;

public class ContactMigrateOptions
{
    public bool Open { get; set; } = false;
    public int Period { get; set; } = 10;
    public int MigrateCount { get; set; } = 50;
    public string UserId { get; set; }
}