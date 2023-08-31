using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractThirdPartNftOrderProcessor : IThirdPartNftOrderProcessor
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AbstractThirdPartNftOrderProcessor> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly ThirdPartOptions _thirdPartOptions;


    protected AbstractThirdPartNftOrderProcessor(ILogger<AbstractThirdPartNftOrderProcessor> logger,
        IClusterClient clusterClient, IThirdPartOrderProvider thirdPartOrderProvider,
        IOptions<ThirdPartOptions> thirdPartOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _thirdPartOptions = thirdPartOptions.Value;
    }


    /// <see cref="ThirdPartNameType"/>
    public abstract string ThirdPartName();

    /// <summary>
    ///     verify order data, and fill Id param (e.g.: verify signature or decrypt data)
    /// </summary>
    /// <param name="input"></param>
    /// <returns>decrypted and verified data</returns>
    public abstract IThirdPartValidOrderUpdateRequest VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input);

    /// <summary>
    ///     Query new order via ThirdPart API and verify
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public abstract Task<IThirdPartValidOrderUpdateRequest> QueryNftOrderAsync(Guid orderId);
    
    /// <summary>
    ///     fill new param-value of orderGrainDto
    /// </summary>
    /// <param name="input"></param>
    /// <param name="orderGrainDto"></param>
    /// <returns> false: no data filled;  true: new data filled </returns>
    public virtual bool FillOrderData(IThirdPartValidOrderUpdateRequest input, OrderGrainDto orderGrainDto)
    {
        // default : do nothing
        return false;
    }

    /// <summary>
    ///     fill new param-value of orderGrainDto
    /// </summary>
    /// <param name="input"></param>
    /// <param name="nftOrderGrainDto"></param>
    /// <returns> false: no data filled;  true: new data filled </returns>
    public virtual bool FillNftOrderData(IThirdPartValidOrderUpdateRequest input, NftOrderGrainDto nftOrderGrainDto)
    {
        // default : do nothing
        return false;
    }

    /// <summary>
    ///     notify thirdPart nft release result
    /// </summary>
    /// <param name="nftOrderGrainDto"></param>
    /// <param name="orderGrainDto"></param>
    /// <returns></returns>
    public abstract Task<CommonResponseDto<Empty>> DoNotifyNftReleaseAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto);

    /// <summary>
    ///     Verify and update nft order
    ///         - verify or decode untrusted order data to trusted data.
    ///         - invoke DoUpdateNftOrderAsync method to update order.
    /// </summary>
    /// <param name="input"> UNVERIFIED and UNTRUSTED order data.</param>
    /// <returns></returns>
    public async Task<CommonResponseDto<Empty>> UpdateThirdPartNftOrderAsync(IThirdPartNftOrderUpdateRequest input)
    {
        var updateRequest = (IThirdPartValidOrderUpdateRequest)null;
        try
        {
            // verify webhook input
            updateRequest = VerifyNftOrderAsync(input);
            AssertHelper.NotEmpty(updateRequest.Id, "Order id missing");
            AssertHelper.NotEmpty(updateRequest.Status, "Order status missing");
            
            await DoUpdateNftOrderAsync(updateRequest);

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

    /// <summary>
    ///     Query and update nft order
    ///         - Query latest order data via ThirdPart API.
    ///         - invoke DoUpdateNftOrderAsync method to update order.
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    public async Task<CommonResponseDto<Empty>> RefreshThirdPartNftOrderAsync(Guid orderId)
    {
        try
        {
            // query latest order data and verify
            var latestValidOrder = await QueryNftOrderAsync(orderId);
            AssertHelper.IsTrue(latestValidOrder.Id != Guid.Empty, "Order id missing");
            AssertHelper.NotEmpty(latestValidOrder.Status, "Order status missing");
            
            await DoUpdateNftOrderAsync(latestValidOrder);

            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("NFT order refresh FAILED, {ThirdPartName}-{OrderId}", ThirdPartName(),
                orderId);
            return new CommonResponseDto<Empty>().Error(e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFT order refresh ERROR, {ThirdPartName}-{OrderId}", ThirdPartName(),
                orderId);
            return new CommonResponseDto<Empty>().Error(e, "INTERNAL ERROR, please try again later.");
        }
    }

    /// <summary>
    ///     do update nft order
    ///         - query base-order data and verify status.
    ///         - query nft-order data and verify.
    ///         - fill new param-value for base-order.
    ///         - fill new param-value for nft-order.
    ///         - update base-order grain and publish dto to write data to ES.
    ///         - update nft-order grain and publish dto to write data to ES.
    /// </summary>
    /// <param name="updateRequest"> VERIFIED and TRUSTED new order data. </param>
    /// <returns></returns>
    private async Task DoUpdateNftOrderAsync(IThirdPartValidOrderUpdateRequest updateRequest)
    {
        var orderId = updateRequest.Id;

        // query verify order grain
        var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
        var orderGrainDto = (await orderGrain.GetOrder()).Data;
        AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);
        AssertHelper.IsTrue(orderGrainDto.Id == updateRequest.Id, "Invalid orderId {ThirdPartOrderId}", updateRequest.Id);
        var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
        if (orderGrainDto.Status == updateRequest.Status)
        {
            _logger.LogInformation("Status {Status} of order {GrainId} no need to update", updateRequest.Status,
                updateRequest.Id);
            return;
        }

        // query nft-order data and verify
        var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
        var nftOrderGrainDto = (await nftOrderGrain.GetNftOrder()).Data;
        AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);
        AssertHelper.IsTrue(nftOrderGrainDto.Id == updateRequest.Id, "Invalid nftOrderId {OrderId}", updateRequest.Id);

        // fill data value and verify new status if necessary
        var orderNeedUpdate = FillOrderData(updateRequest, orderGrainDto);
        var nftOrderNeedUpdate = FillNftOrderData(updateRequest, nftOrderGrainDto);

        // update nft order grain first
        if (nftOrderNeedUpdate)
        {
            // update nft order grain
            var nftOrderUpdateResult = await _thirdPartOrderProvider.UpdateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(nftOrderUpdateResult.Success, "Update nft order fail");
        }

        // update order grain
        if (orderNeedUpdate)
        {
            var nextStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
            AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, nextStatus),
                "Status {Next} unreachable from {Current}", nextStatus, currentStatus);
            var orderUpdateResult = await _thirdPartOrderProvider.UpdateRampOrderAsync(orderGrainDto);
            AssertHelper.IsTrue(orderUpdateResult.Success, "Update ramp order fail");
        }
    }

    public async Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId)
    {
        var maxNotifyCount = _thirdPartOptions.Timer.NftCheckoutResultThirdPartNotifyCount;
        try
        {
            // query nft-order data and verify
            var nftOrderGrain = _clusterClient.GetGrain<INftOrderGrain>(orderId);
            var nftOrderGrainDto = (await nftOrderGrain.GetNftOrder()).Data;
            AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);
            AssertHelper.IsTrue(nftOrderGrainDto.ThirdPartNotifyCount < maxNotifyCount,
                "Notify max count reached : " + maxNotifyCount);

            // query verify order grain
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(orderId);
            var orderGrainDto = (await orderGrain.GetOrder()).Data;
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            var notifyThirdPartResp = await DoNotifyNftReleaseAsync(orderGrainDto, nftOrderGrainDto);

            nftOrderGrainDto.ThirdPartNotifyCount ++;
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

            var nftOrderUpdateResult = await _thirdPartOrderProvider.UpdateNftOrderAsync(nftOrderGrainDto);
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