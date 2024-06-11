namespace CAServer.RedPackage.Dtos;

public class PreGrabbedItemDto
{
    public string Username { get; set; }
    public long GrabTime { get; set; }
    public string Amount { get; set; }
    public int Decimal { get; set; }
    public long ExpirationTime { get; set; }
}