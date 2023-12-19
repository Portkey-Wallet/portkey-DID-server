namespace CAServer.Telegram.Dtos;

public class TelegramAuthDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string AuthDate { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Hash { get; set; }
    public string ProtoUrl { get; set; }
}