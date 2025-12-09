namespace CAServer.Admin.Dtos;

public class GetRampOrderRequest
{
    
    public string TransDirect { get; set; }
    public string MerchantName { get; set; }
    public string LastModifyTimeGtEq { get; set; }
    public string LastModifyTimeLt { get; set; }
    public string ThirdPartOrderNo { get; set; }
    public string OrderId { get; set; }
    public string UserId { get; set; }
    public string TransactionId { get; set; }
    public string Status { get; set; }
    
}