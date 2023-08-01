using System;
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
    public TransakOrderProcessor(IClusterClient clusterClient, ILogger<TransakOrderProcessor> logger,
        IThirdPartOrderProvider thirdPartOrderProvider, IDistributedEventBus distributedEventBus,
        IOrderStatusProvider orderStatusProvider, IObjectMapper objectMapper) : base(clusterClient, logger,
        thirdPartOrderProvider, distributedEventBus, orderStatusProvider, objectMapper)
    {
    }


    protected override void VerifyOrderInput<T>(T iThirdPartOrder)
    {
        //TODO
        throw new NotImplementedException();
    }

    protected override OrderDto ConvertOrderDto<T>(T iThirdPartOrder)
    {
        //TODO
        throw new NotImplementedException();
    }

    public override string MapperOrderStatus(OrderDto orderDto)
    {
        //TODO
        throw new NotImplementedException();
    }

    public override string MerchantName()
    {
        //TODO
        return MerchantNameType.Transak.ToString();
    }

    public override Task UpdateTxHashAsync(TransactionHashDto transactionHashDto)
    {
        //TODO
        throw new NotImplementedException();
    }

    public override Task<OrderDto> QueryThirdOrder(OrderDto orderDto)
    {
        throw new NotImplementedException();
    }
}