namespace CAServer.Telegram.Dtos;

public class TelegramAuthResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}