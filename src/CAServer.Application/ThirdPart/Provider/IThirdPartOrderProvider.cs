using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;

namespace CAServer.ThirdPart.Provider;

public interface IThirdPartOrderProvider
{
    public Task<CommonResponseDto<Empty>> UpdateRampOrderAsync(OrderGrainDto dataToBeUpdated);
    public Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(NftOrderGrainDto dataToBeUpdated);
    public Task<int> CallBackNftOrderPayResultAsync(Guid orderId, string callbackStatus);
    
    public Task<RampOrderIndex> GetThirdPartOrderIndexAsync(string orderId);
    public Task<OrderDto> GetThirdPartOrderAsync(string orderId);
    public Task<List<OrderDto>> GetUnCompletedThirdPartOrdersAsync();
    public Task<PageResultDto<OrderDto>> GetThirdPartOrdersByPageAsync(GetThirdPartOrderConditionDto condition, params OrderSectionEnum?[] withSections);
    public Task<PageResultDto<OrderDto>> GetNftOrdersByPageAsync(NftOrderQueryConditionDto condition);

    public void SignMerchantDto(NftMerchantBaseDto input);
    public void VerifyMerchantSignature(NftMerchantBaseDto input);
}