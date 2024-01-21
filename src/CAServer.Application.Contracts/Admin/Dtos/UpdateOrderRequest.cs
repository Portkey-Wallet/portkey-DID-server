using CAServer.ThirdPart.Dtos;

namespace CAServer.Admin.Dtos;

public class UpdateOrderRequest
{
    
    public string TfaPin { get; set; }
    public OrderDto OrderDto { get; set; }
    
}