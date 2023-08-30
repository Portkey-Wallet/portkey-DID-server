using System;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartOrderProcessor : ISingletonDependency
{
    public string ThirdPartName();

    public Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(IThirdPartNftOrderUpdateRequest input);

    public Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId);
    
}