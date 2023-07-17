namespace CAServer.ThirdPart.Dtos;

public class OrderStatusInfo
{
    public OrderStatusType Status { get; set; }
    public long LastModifyTime { get; set; }
    public string Extension { get; set; }
}