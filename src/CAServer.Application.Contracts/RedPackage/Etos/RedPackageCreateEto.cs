using System;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;

[EventName("RedPackageCreateEto")]
public class RedPackageCreateEto
{
    public Guid? UserId { get; set; }
    public string ChainId { get; set; }
    public Guid SessionId { get; set; }
    public string RawTransaction { get; set; }
    public Guid RedPackageId { get; set; }
  

}