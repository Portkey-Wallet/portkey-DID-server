using System.Security.Cryptography;
using System.Text;
using CAServer.Commons;
using CAServer.Grains.State.Order;
using CAServer.ThirdPart;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public interface IPendingTreasuryOrderGrain : IGrainWithGuidKey
{
    public static Guid GenerateId(string thirdPartName, string thirdPartOrderId)
    {
        return new Guid(MD5.HashData(Encoding.Default.GetBytes(string.Join("_", thirdPartName, thirdPartOrderId))));
    }

    Task<PendingTreasuryOrderDto> AddOrUpdateAsync(PendingTreasuryOrderDto pendingData);

    Task<PendingTreasuryOrderDto> GetAsync();
}

public class PendingTreasuryOrderGrain : Grain<PendingTreasuryOrderState>, IPendingTreasuryOrderGrain
{
    private readonly ILogger<PendingTreasuryOrderGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    public PendingTreasuryOrderGrain(IObjectMapper objectMapper, ILogger<PendingTreasuryOrderGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<PendingTreasuryOrderDto> AddOrUpdateAsync(PendingTreasuryOrderDto pendingData)
    {
        var createTime = State.CreateTime;
        _objectMapper.Map(pendingData, State);
        State.CreateTime = createTime == 0 ? DateTime.UtcNow.ToUtcMilliSeconds() : createTime;
        State.LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.Id = this.GetPrimaryKey();
        await WriteStateAsync();
        return _objectMapper.Map<PendingTreasuryOrderState, PendingTreasuryOrderDto>(State);
    }

    public Task<PendingTreasuryOrderDto> GetAsync()
    {
        return State.Id == Guid.Empty
            ? null
            : Task.FromResult(_objectMapper.Map<PendingTreasuryOrderState, PendingTreasuryOrderDto>(State));
    }
}