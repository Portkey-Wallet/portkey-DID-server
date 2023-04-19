using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart.Provider;

public interface IThirdPartOrderProvider
{
    public Task<OrderDto> GetThirdPartOrderAsync(string orderId);
    public Task<List<OrderDto>> GetThirdPartOrdersByPageAsync(Guid userId, int skipCount, int maxResultCount);
}