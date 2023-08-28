using System;
using CAServer.ThirdPart.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.ThirdPart.Etos;

[EventName("NFTOrderEto")]
public class NftOrderEto : NftOrderSectionDto
{

}