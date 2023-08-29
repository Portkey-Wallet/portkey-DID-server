using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractThirdPartOrderProcessor : IThirdPartOrderProcessor
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AbstractThirdPartOrderProcessor> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;


    protected AbstractThirdPartOrderProcessor(ILogger<AbstractThirdPartOrderProcessor> logger,
        IClusterClient clusterClient, IThirdPartOrderProvider thirdPartOrderProvider)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _thirdPartOrderProvider = thirdPartOrderProvider;
    }


    /// <see cref="ThirdPartNameType"/>
    public abstract string ThirdPartName();

    /// <summary>
    ///     verify order data, and fill Id param (e.g.: verify signature or decrypt data)
    /// </summary>
    /// <param name="input"></param>
    /// <returns>decrypted and verified data</returns>
    public abstract IThirdPartNftOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input);

    /// <summary>
    ///     fill new param-value of orderGrainDto
    /// </summary>
    /// <param name="input"></param>
    /// <param name="orderGrainDto"></param>
    public abstract void FillOrderData(IThirdPartNftOrderUpdateRequest input, OrderGrainDto orderGrainDto);

    /// <summary>
    ///     fill new param-value of orderGrainDto
    /// </summary>
    /// <param name="input"></param>
    /// <param name="orderGrainDto"></param>
    public abstract void FillNftOrderData(IThirdPartNftOrderUpdateRequest input, NftOrderGrainDto orderGrainDto);

    /// <summary>
    ///     do update nft order
    ///         Step1: verify webhook input (e.g.: verify signature or decrypt data)
    ///         Step2: query base-order data and verify status
    ///         Step3: query nft-order data and verify
    ///         Step4: fill new param-value for base-order
    ///         Step5: fill new param-value for nft-order
    ///         Step6: update base-order grain and publish dto to write data to ES
    ///         Step7: update nft-order grain and publish dto to write data to ES
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<Empty>> UpdateNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        var updateRequest = (IThirdPartNftOrderUpdateRequest)null;
        try
        {
            // verify webhook input
            updateRequest = VerifyNftOrderAsync(input);
            AssertHelper.IsTrue(updateRequest.Id != Guid.Empty, "Order id required");

            var orderId = updateRequest.Id;
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);

            // query verify order grain
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);
            AssertHelper.IsTrue(orderGrainDto.Id == input.Id, "Invalid orderId {ThirdPartOrderId}", input.Id);
            if (orderGrainDto.Status == input.Status)
            {
                _logger.LogInformation("Status {Status} of order {GrainId} no need to update", updateRequest.Status,
                    updateRequest.Id);
                return new CommonResponseDto<Empty>();
            }

            // query nft-order data and verify
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = (await nftOrderGrain.GetNftOrder()).Data;
            AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);
            AssertHelper.IsTrue(nftOrderGrainDto.Id == input.Id, "Invalid nftOrderId {OrderId}", input.Id);

            // fill data value
            FillOrderData(input, orderGrainDto);
            FillNftOrderData(input, nftOrderGrainDto);

            // update order grain
            var orderUpdateResult = await _thirdPartOrderProvider.DoUpdateRampOrderAsync(orderGrainDto);
            AssertHelper.IsTrue(orderUpdateResult.Success, "Update ramp order fail");

            // update nft order grain
            var nftOrderUpdateResult = await _thirdPartOrderProvider.DoUpdateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(nftOrderUpdateResult.Success, "Update nft order fail");

            return orderUpdateResult;
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("NFT order update FAILED, {ThirdPartName}-{OrderId}", ThirdPartName(),
                updateRequest?.Id);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFT order update ERROR, {ThirdPartName}-{OrderId}", ThirdPartName(),
                updateRequest?.Id);
            return new CommonResponseDto<Empty>().Error(e, "INTERNAL ERROR, please try again later.");
        }
    }
}