using System;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class OrderStatusProviderTest : CAServerApplicationTestBase
{
    private readonly IOrderStatusProvider _orderStatusProvider;

    public OrderStatusProviderTest()
    {
        _orderStatusProvider = GetRequiredService<IOrderStatusProvider>();
    }

    [Fact]
    public async Task AddOrderStatusInfoAsync()
    {
        await _orderStatusProvider.AddOrderStatusInfoAsync(new OrderStatusInfoGrainDto()
        {
            Id = "test",
            OrderId = Guid.Empty,
            ThirdPartOrderNo = Guid.NewGuid().ToString(),
            RawTransaction = "test",
            OrderStatusInfo = new OrderStatusInfo()
            {
                Status = "Created",
                LastModifyTime = DateTime.UtcNow.Microsecond
            }
        });
    }

    [Fact]
    public async Task UpdateOrderStatus_GetNull_Async()
    {
        await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto()
        {
            OrderId = "test",
            RawTransaction = "test",
            Order = new OrderDto
            {
                Id = Guid.Empty,
                Status = "Created"
            }
        });
    }

    [Fact]
    public async Task UpdateOrderStatusAsync()
    {
        var orderId = Guid.NewGuid();
        await _orderStatusProvider.AddOrderStatusInfoAsync(new OrderStatusInfoGrainDto()
        {
            Id = orderId.ToString(),
            OrderId = orderId,
            ThirdPartOrderNo = Guid.NewGuid().ToString(),
            RawTransaction = "test",
            OrderStatusInfo = new OrderStatusInfo()
            {
                Status = "Created",
                LastModifyTime = DateTime.UtcNow.Microsecond
            }
        });
        await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto()
        {
            OrderId = orderId.ToString(),
            RawTransaction = "test",
            Order = new OrderDto
            {
                Id = orderId,
                Status = "Created"
            }
        });
    }
}