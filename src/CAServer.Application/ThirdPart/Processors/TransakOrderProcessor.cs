using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processors;

public class TransakOrderProcessor : AbstractOrderProcessor
{
    public TransakOrderProcessor(IClusterClient clusterClient, ILogger<AbstractOrderProcessor> logger, IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus, IOrderStatusProvider orderStatusProvider, IObjectMapper objectMapper) : base(clusterClient, logger, thirdPartOrderProvider, distributedEventBus, orderStatusProvider, objectMapper)
    {
    }

    public override string MerchantName()
    {
        throw new System.NotImplementedException();
    }

    protected override void VerifyOrderInput<T>(T orderDto)
    {
        throw new System.NotImplementedException();
    }

    public override Task UpdateTxHashAsync(TransactionHashDto transactionHashDto)
    {
        throw new System.NotImplementedException();
    }

    public override Task<T> QueryThirdOrder<T>(T orderDto)
    {
        throw new System.NotImplementedException();
    }
}