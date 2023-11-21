using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace CAServer.RedPackage.Etos;

[EventName("RedPackageCreateEto")]
public class RedPackageCreateEto
{
    public Guid? UserId { get; set; }
    public string ChainId { get; set; }
    public Guid SessionId { get; set; }
    public string RawTransaction { get; set; }
    public string Symbol { get; set; }
    public List<GrabItemDto> Items { get; set; }
    
    public Guid RedPackageId { get; set; }
    public class GrabItemDto
    {
        public string Amount { get; set; }
        public string CaAddress { get; set; }
        public Guid UserId { get; set; }
        public bool PaymentCompleted{ get; set; }
    }

}