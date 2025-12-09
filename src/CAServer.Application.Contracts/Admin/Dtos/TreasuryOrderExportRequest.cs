using CAServer.ThirdPart.Dtos.Order;

namespace CAServer.Admin.Dtos;

public class TreasuryOrderExportRequest : MfaRequest<TreasuryOrderCondition>
{

    public int TimeZone { get; set; } = 0;

}