using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.ThirdPart;

public partial class ThirdPartOrderAppService
{
    
    public Task<CommonResponseDto<RampCoverage>> GetRampCoverageAsync(string type)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampDetail>> GetRampDetailAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<RampProviderDetail>> GetRampProvidersDetailAsync(RampDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<CommonResponseDto<Empty>> TransactionForwardCall(TransactionDto input)
    {
        
        throw new NotImplementedException();
    }
    
    
}