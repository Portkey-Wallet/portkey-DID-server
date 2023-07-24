using System;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using Moq;
using Orleans;

namespace CAServer.ThirdPart.Alchemy;

public partial class OrderStatusProviderTest
{
    private IClusterClient GetOrderGrain()
    {
        var orderGrain = new Mock<IClusterClient>();
        //orderGrain.Setup(m => m.GetGrain<IOrderGrain>(It.IsAny<Guid>(),It.IsAny<string>())).ReturnsAsync();

        return orderGrain.Object;
    }
}