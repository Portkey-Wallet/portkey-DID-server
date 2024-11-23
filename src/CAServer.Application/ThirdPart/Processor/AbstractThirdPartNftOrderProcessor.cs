using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Provider;
using CAServer.Tokens;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.DistributedLocking;

namespace CAServer.ThirdPart.Processor;

public abstract class AbstractThirdPartNftOrderProcessor : IThirdPartNftOrderProcessor
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AbstractThirdPartNftOrderProcessor> _logger;
    private readonly IOrderStatusProvider _orderStatusProvider;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;

    private static readonly JsonSerializerSettings JsonSerializerSettings = JsonSettingsBuilder.New()
        .WithAElfTypesConverters()
        .WithCamelCasePropertyNamesResolver()
        .IgnoreNullValue()
        .Build();

    protected AbstractThirdPartNftOrderProcessor(ILogger<AbstractThirdPartNftOrderProcessor> logger,
        IClusterClient clusterClient,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions,
        IOrderStatusProvider orderStatusProvider, IContractProvider contractProvider,
        IAbpDistributedLock distributedLock, IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _orderStatusProvider = orderStatusProvider;
        _contractProvider = contractProvider;
        _distributedLock = distributedLock;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _thirdPartOptions = thirdPartOptions;
    }


    /// <see cref="ThirdPartNameType"/>
    public abstract string ThirdPartName();

    /// <summary>
    ///     verify order data, and fill Id param (e.g.: verify signature or decrypt data)
    /// </summary>
    /// <param name="input"></param>
    /// <returns>decrypted and verified data</returns>
    public abstract Task<IThirdPartValidOrderUpdateRequest> VerifyNftOrderAsync(IThirdPartNftOrderUpdateRequest input);

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
    ///     Calculate order settlement data of ThirdPart
    /// </summary>
    /// <param name="orderGrainDto"></param>
    /// <returns></returns>
    public abstract Task<OrderSettlementGrainDto> FillOrderSettlementAsync(OrderGrainDto orderGrainDto,
        NftOrderGrainDto nftOrderGrainDto, OrderSettlementGrainDto orderSettlementGrainDto, long? finishTime = null);

    /// <summary>
    ///     Verify and update nft order
    ///         - verify or decode untrusted order data to trusted data.
    ///         - invoke DoUpdateNftOrderAsync method to update order.
    /// </summary>
    /// <param name="input"> UNVERIFIED and UNTRUSTED order data.</param>
    /// <returns></returns>
    public async Task<CommonResponseDto<Empty>> UpdateThirdPartNftOrderAsync(IThirdPartNftOrderUpdateRequest request)
    {
        var updateRequest = (IThirdPartValidOrderUpdateRequest)null;
        try
        {
            // verify webhook input
            updateRequest = await VerifyNftOrderAsync(request);
            AssertHelper.NotEmpty(updateRequest.Id, "Order id missing");
            AssertHelper.NotEmpty(updateRequest.Status, "Order status missing");

            await DoUpdateNftOrderAsync(updateRequest);

            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "NFT order update FAILED, {ThirdPartName}-{OrderId}", ThirdPartName(),
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
        var orderGrainDto = await _orderStatusProvider.GetRampOrderAsync(orderId);
        AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);
        var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);

        // query nft-order data and verify
        var nftOrderGrainDto = await _orderStatusProvider.GetNftOrderAsync(orderId);
        AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);

        // fill data value and verify new status if necessary
        var orderNeedUpdate = FillOrderData(updateRequest, orderGrainDto);
        var nftOrderNeedUpdate = FillNftOrderData(updateRequest, nftOrderGrainDto);

        // update nft order grain first
        if (nftOrderNeedUpdate)
        {
            // update nft order grain
            var nftOrderUpdateResult = await _orderStatusProvider.UpdateNftOrderAsync(nftOrderGrainDto);
            AssertHelper.IsTrue(nftOrderUpdateResult.Success, "Update nft order fail");
        }

        // update order grain
        if (orderNeedUpdate)
        {
            var nextStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
            AssertHelper.IsTrue(OrderStatusTransitions.Reachable(currentStatus, nextStatus),
                "Status {Next} unreachable from {Current}", nextStatus, currentStatus);
            orderGrainDto.MerchantName = orderGrainDto.MerchantName.DefaultIfEmpty(ThirdPartName());
            var orderUpdateResult = await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto);
            AssertHelper.IsTrue(orderUpdateResult.Success, "Update ramp order fail");
        }
    }

    /// <summary>
    ///     Send settlement transfer to merchant receiving address
    /// </summary>
    /// <param name="orderId"></param>
    public async Task SettlementTransferAsync(Guid orderId)
    {
        const string lockKeyPrefix = "nft_order:settlement_transfer:";
        try
        {
            // Duplicate check
            await using var distributedLock = await _distributedLock.TryAcquireAsync(lockKeyPrefix + orderId);
            AssertHelper.NotNull(distributedLock, "Duplicate transfer, abort");

            // query verify order grain
            var orderGrainDto = await _orderStatusProvider.GetRampOrderAsync(orderId);
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);
            AssertHelper.IsTrue(currentStatus == OrderStatusType.StartTransfer,
                "Order not in settlement StartTransfer state, current: {Status}", currentStatus);

            // query nft-order data and verify
            var nftOrderGrainDto = await _orderStatusProvider.GetNftOrderAsync(orderId);
            AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);
            AssertHelper.NotEmpty(nftOrderGrainDto.MerchantAddress, "NFT order merchant address missing");

            // generate transfer transaction or use an old transaction data
            Transaction transferTx;
            if (orderGrainDto.RawTransaction.IsNullOrEmpty())
            {
                var amount = orderGrainDto.CryptoAmount.SafeToDecimal() * (decimal)Math.Pow(10, orderGrainDto.CryptoDecimals);
                (var txId, transferTx) = await _contractProvider.GenerateTransferTransactionAsync(orderGrainDto.Crypto,
                    amount.ToString(0, DecimalHelper.RoundingOption.Floor),
                    nftOrderGrainDto.MerchantAddress, CommonConstant.MainChainId,
                    _thirdPartOptions.CurrentValue.Merchant.NftOrderSettlementPublicKey);
                // update main-order, record transactionId first
                orderGrainDto.TransactionId = txId;
                orderGrainDto.RawTransaction = transferTx.ToByteArray().ToHex();
            }
            else
            {
                transferTx =
                    Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(orderGrainDto.RawTransaction));
            }
            
            orderGrainDto.Status = OrderStatusType.Transferring.ToString();
            var transferringExtension = OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.TxHash, orderGrainDto.TransactionId)
                .Add(ExtensionKey.Transaction, JsonConvert.SerializeObject(transferTx, JsonSerializerSettings))
                .Build();
            var transferringResult =
                await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto, transferringExtension);
            AssertHelper.IsTrue(transferringResult.Success, "Save ramp order failed: " + transferringResult.Message);

            // Transfer crypto to merchant, and wait result
            var sendResult =
                await _contractProvider.SendRawTransactionAsync(CommonConstant.MainChainId,
                    transferTx.ToByteArray().ToHex());
            AssertHelper.NotNull(sendResult, "Empty send result");

            var txResult = await WaitTransactionResultAsync(CommonConstant.MainChainId, sendResult.TransactionId);
            AssertHelper.NotNull(txResult, "Transaction result empty, {ChainId}-{TxId}", CommonConstant.MainChainId,
                sendResult.TransactionId);
            AssertHelper.IsTrue(txResult.Status != TransactionState.NodeValidationFailed,
                "Settlement transfer failed: " + txResult.Error);

            // update main-order
            orderGrainDto.TransactionId = sendResult.TransactionId;
            orderGrainDto.Status = TransactionState.IsStateSuccessful(txResult.Status)
                ? OrderStatusType.Transferred.ToString()
                : OrderStatusType.Transferring.ToString();

            var resExtensionBuilder = OrderStatusExtensionBuilder.Create()
                .Add(ExtensionKey.TxStatus, txResult.Status)
                .Add(ExtensionKey.TxBlockHeight, txResult.BlockNumber.ToString());
            if (! TransactionState.IsStateSuccessful(txResult.Status))
                resExtensionBuilder.Add(ExtensionKey.TxResult,
                    JsonConvert.SerializeObject(txResult, JsonSerializerSettings));

            var updateResult =
                await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto, resExtensionBuilder.Build());
            AssertHelper.IsTrue(updateResult.Success, "Save ramp order failed: " + updateResult.Message);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "Send SettlementTransferAsync failed, orderId={OrderId}", orderId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Send SettlementTransferAsync error, orderId={OrderId}", orderId);
        }
    }

    /// <summary>
    ///     Save order settlement 
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="finishTime"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<CommonResponseDto<Empty>> SaveOrderSettlementAsync(Guid orderId, long? finishTime = null)
    {
        try
        {
            // query verify order grain
            var orderGrainDto = await _orderStatusProvider.GetRampOrderAsync(orderId);
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            // query nft-order data and verify
            var nftOrderGrainDto = await _orderStatusProvider.GetNftOrderAsync(orderId);
            AssertHelper.NotNull(nftOrderGrainDto, "No nft order found for {OrderId}", orderId);

            // fill order settlement data and save
            var orderSettlementGrainDto = await _thirdPartOrderAppService.GetOrderSettlementAsync(orderId);
            await FillOrderSettlementAsync(orderGrainDto, nftOrderGrainDto, orderSettlementGrainDto, finishTime);

            // save
            await _thirdPartOrderAppService.AddUpdateOrderSettlementAsync(orderSettlementGrainDto);

            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("NFT order SaveOrderSettlementAsync, orderId={OrderId}, msg={Msg}", orderId,
                e.Message);
            return new CommonResponseDto<Empty>().Error(e, e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFT order SaveOrderSettlementAsync error, orderId={OrderId}", orderId);
            return new CommonResponseDto<Empty>().Error(e, e.Message);
        }
    }

    /// <summary>
    ///     InvokeAsync by worker, refresh settlement transfer transaction status
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="confirmedHeight"></param>
    public async Task<CommonResponseDto<Empty>> RefreshSettlementTransferAsync(Guid orderId, long chainHeight,
        long confirmedHeight)
    {
        try
        {
            // query verify order grain
            var orderGrainDto = await _orderStatusProvider.GetRampOrderAsync(orderId);
            AssertHelper.NotNull(orderGrainDto, "No order found for {OrderId}", orderId);

            var currentStatus = ThirdPartHelper.ParseOrderStatus(orderGrainDto.Status);

            // something wrong before transfer sent, status will be stay StartTransfer
            if (currentStatus == OrderStatusType.StartTransfer)
            {
                await SettlementTransferAsync(orderId);
                return new CommonResponseDto<Empty>();
            }

            var transferringState = new List<OrderStatusType>
            {
                OrderStatusType.Transferring,
                OrderStatusType.Transferred
            };
            AssertHelper.IsTrue(transferringState.Contains(currentStatus),
                "Order not in settlement Transferring state, current: {Status}", currentStatus);
            AssertHelper.NotEmpty(orderGrainDto.TransactionId, "TransactionId not exists {OrderId}", orderId);

            // Query transfer transaction status
            var rawTxResult =
                await _contractProvider.GetTransactionResultAsync(CommonConstant.MainChainId,
                    orderGrainDto.TransactionId);
            _logger.LogInformation(
                "RefreshSettlementTransferAsync, orderId={OrderId}, transactionId={TransactionId}, status={Status}, block={Height}",
                orderId, orderGrainDto.TransactionId, rawTxResult.Status, rawTxResult.BlockNumber);
            AssertHelper.IsTrue(rawTxResult.Status != TransactionState.Pending, "Transaction still pending status.");

            // update order status
            var newStatus = TransactionState.IsStateSuccessful(rawTxResult.Status)
                ? rawTxResult.BlockNumber <= confirmedHeight || chainHeight >=
                rawTxResult.BlockNumber + _thirdPartOptions.CurrentValue.Timer.TransactionConfirmHeight
                    ? OrderStatusType.Finish.ToString()
                    : OrderStatusType.Transferred.ToString()
                : OrderStatusType.TransferFailed.ToString();
            AssertHelper.IsTrue(orderGrainDto.Status != newStatus,
                "Order status not changed, status={Status}, txBlock={Height}",
                rawTxResult.Status, rawTxResult.BlockNumber);

            // Record transfer data when filed
            var extraInfo = newStatus == OrderStatusType.TransferFailed.ToString()
                ? OrderStatusExtensionBuilder.Create()
                    .Add(ExtensionKey.TxResult, JsonConvert.SerializeObject(rawTxResult, JsonSerializerSettings))
                    .Build()
                : null;

            // update order status
            orderGrainDto.Status = newStatus;
            var updateRes = await _orderStatusProvider.UpdateRampOrderAsync(orderGrainDto, extraInfo);
            AssertHelper.IsTrue(updateRes.Success, "Update NFT order settlement status failed: {Msg}",
                updateRes.Message);
            return new CommonResponseDto<Empty>();
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning("NFT order RefreshSettlementTransferAsync not change, orderId={OrderId}, msg={Msg}", orderId,
                e.Message);
            return new CommonResponseDto<Empty>().Error(e, e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "NFT order RefreshSettlementTransferAsync error, orderId={OrderId}", orderId);
            return new CommonResponseDto<Empty>().Error(e, e.Message);
        }
    }

    private async Task<TransactionResultDto> WaitTransactionResultAsync(string chainId, string transactionId)
    {
        var waitingStatus = new List<string> { TransactionState.NotExisted, TransactionState.Pending };
        var maxWaitMillis = _thirdPartOptions.CurrentValue.Timer.TransactionWaitTimeoutSeconds * 1000;
        var delayMillis = _thirdPartOptions.CurrentValue.Timer.TransactionWaitDelaySeconds * 1000;
        TransactionResultDto rawTxResult = null;
        using var cts = new CancellationTokenSource(maxWaitMillis);
        try
        {
            while (!cts.IsCancellationRequested && (rawTxResult == null || waitingStatus.Contains(rawTxResult.Status)))
            {
                // delay some times
                await Task.Delay(delayMillis, cts.Token);

                rawTxResult = await _contractProvider.GetTransactionResultAsync(chainId, transactionId);
                _logger.LogDebug(
                    "WaitTransactionResultAsync chainId={ChainId}, transactionId={TransactionId}, status={Status}", chainId,
                    transactionId, rawTxResult.Status);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Timed out waiting for transactionId {TransactionId} result", transactionId);
        }

        return rawTxResult;
    }


    public async Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId)
    {
        var maxNotifyCount = _thirdPartOptions.CurrentValue.Timer.NftCheckoutResultThirdPartNotifyCount;
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

            // notify third-part NFT released
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

            var nftOrderUpdateResult = await _orderStatusProvider.UpdateNftOrderAsync(nftOrderGrainDto);
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