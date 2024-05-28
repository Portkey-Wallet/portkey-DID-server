using System;

namespace CAServer.DataReporting.Etos;

public class ExitWalletEto
{
    public Guid UserId { get; set; }
    public string DeviceId { get; set; }
}