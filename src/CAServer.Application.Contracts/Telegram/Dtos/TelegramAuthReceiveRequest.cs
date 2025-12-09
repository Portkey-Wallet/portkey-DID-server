namespace CAServer.Telegram.Dtos;

public class TelegramAuthReceiveRequest
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Auth_Date { get; set; }
    public string First_Name { get; set; }
    public string Last_Name { get; set; }
    public string Hash { get; set; }
    public string Photo_Url { get; set; }
}