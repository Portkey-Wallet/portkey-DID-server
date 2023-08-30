using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
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
    /// <returns> false: no data filled;  true: new data filled </returns>
    public abstract bool FillOrderData(IThirdPartNftOrderUpdateRequest input, OrderGrainDto orderGrainDto);

    /// <summary>
    ///     fill new param-value of orderGrainDto
    /// </summary>
    /// <param name="input"></param>
    /// <param name="nftOrderGrainDto"></param>
    /// <returns> false: no data filled;  true: new data filled </returns>
    public abstract bool FillNftOrderData(IThirdPartNftOrderUpdateRequest input, NftOrderGrainDto nftOrderGrainDto);

    /// <summary>
    ///     notify thirdPart nft release result
    /// </summary>
    /// <param name="nftOrderGrainDto"></param>
    /// <param name="orderGrainDto"></param>
    /// <returns></returns>
    public abstract Task<CommonResponseDto<Empty>> DoNotifyNftReleaseAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto);


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

            // query verify order grain
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);
            AssertHelper.IsTrue(orderGrainDto.Id == input.Id, "Invalid orderId {ThirdPartOrderId}", input.Id);
            var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
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

            // fill data value and verify new status
            var orderNeedUpdate = FillOrderData(input, orderGrainDto);
            var nftOrderNeedUpdate = FillNftOrderData(input, nftOrderGrainDto);

            // update nft order grain first
            if (nftOrderNeedUpdate)
            {
                // update nft order grain
                var nftOrderUpdateResult = await _thirdPartOrderProvider.DoUpdateNftOrderAsync(nftOrderGrainDto);
                AssertHelper.IsTrue(nftOrderUpdateResult.Success, "Update nft order fail");
            }

            // update order grain
            if (orderNeedUpdate)
            {
                var nextStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
                AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, nextStatus),
                    "Status {Next} unreachable from {Current}", nextStatus, currentStatus);
                var orderUpdateResult = await _thirdPartOrderProvider.DoUpdateRampOrderAsync(orderGrainDto);
                AssertHelper.IsTrue(orderUpdateResult.Success, "Update ramp order fail");
            }

            return new CommonResponseDto<Empty>();
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

    public async Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId)
    {
        try
        {
            // query nft-order data and verify
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = (await nftOrderGrain.GetNftOrder()).Data;
            AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);

            // query verify order grain
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            var notifyThirdPartResp = await DoNotifyNftReleaseAsync(orderGrainDto, nftOrderGrainDto);

            nftOrderGrainDto.ThirdPartNotifyCount++;
            nftOrderGrainDto.ThirdPartNotifyTime = DateTime.UtcNow.ToUtcString();
            if (notifyThirdPartResp.Success)
            {
                nftOrderGrainDto.ThirdPartNotifyResult = NftOrderWebhookStatus.SUCCESS.ToString();
                nftOrderGrainDto.ThirdPartNotifyStatus = NftOrderWebhookStatus.SUCCESS.ToString();
            }
            else
            {
                nftOrderGrainDto.ThirdPartNotifyResult =
                    string.Join(":", notifyThirdPartResp.Code, notifyThirdPartResp.Message);
                nftOrderGrainDto.ThirdPartNotifyStatus = NftOrderWebhookStatus.FAIL.ToString();
            }

            var nftOrderUpdateResult = await _thirdPartOrderProvider.DoUpdateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(nftOrderUpdateResult.Success, "Update nft order fail");

            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("NFT result notify FAILED, {ThirdPartName}-{OrderId}", ThirdPartName(), orderId);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFT order update ERROR, {ThirdPartName}-{OrderId}", ThirdPartName(), orderId);
            return new CommonResponseDto<Empty>().Error(e, "INTERNAL ERROR, please try again later.");
        }
    }
}