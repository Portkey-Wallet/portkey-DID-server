using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Types;
using AElf.Kernel;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.ThirdPart.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Alchemy;

[RemoteService(false), DisableAuditing]
public class AlchemyOrderAppService : CAServerAppService, IAlchemyOrderAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AlchemyOrderAppService> _logger;
    private readonly IThirdPartOrderProvider _thirdPartOrderProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IAlchemyProvider _alchemyProvider;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyOrderAppService(IClusterClient clusterClient,
        IThirdPartOrderProvider thirdPartOrderProvider,
        IDistributedEventBus distributedEventBus,
        ILogger<AlchemyOrderAppService> logger,
        IOptions<ThirdPartOptions> merchantOptions,
        IAlchemyProvider alchemyProvider,
        IObjectMapper objectMapper)
    {
        _thirdPartOrderProvider = thirdPartOrderProvider;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
        _alchemyOptions = merchantOptions.Value.alchemy;
        _alchemyProvider = alchemyProvider;
    }

    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        try
        {
            _logger.LogInformation(
                "Update Order OrderNo:{MerchantOrderNo}, MerchantOrderNo:{OrderNo}, Status:{Status}, get from alchemy",
                input.MerchantOrderNo, input.OrderNo, input.Status);

            // if (input.Signature != GetAlchemySignature(input.OrderNo, input.Crypto, input.Network, input.Address))
            // {
            //     _logger.LogError("Alchemy signature check failed, OrderNo: {orderNo} will not update.",
            //         input.OrderNo);
            //     return new BasicOrderResult();
            // }

            Guid grainId = ThirdPartHelper.GetOrderId(input.MerchantOrderNo);
            var esOrderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(grainId.ToString());
            if (esOrderData == null || input.MerchantOrderNo != esOrderData.Id.ToString())
            {
                return new BasicOrderResult() { Message = $"No order found for {grainId}" };
            }

            if (esOrderData.Status == input.Status)
            {
                return new BasicOrderResult() { Message = $"Order status {input.Status} no need to update." };
            }

            var dataToBeUpdated = MergeEsAndInput2GrainModel(input, esOrderData);
            var orderGrain = _clusterClient.GetGrain<IOrderGrain>(grainId);
            dataToBeUpdated.Status = AlchemyHelper.GetOrderStatus(input.Status);
            dataToBeUpdated.Id = grainId;
            dataToBeUpdated.UserId = esOrderData.UserId;
            dataToBeUpdated.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();
            _logger.LogInformation("This alchemy order {grainId} will be updated.", grainId);

            var result = await orderGrain.UpdateOrderAsync(dataToBeUpdated);

            if (!result.Success)
            {
                _logger.LogError("Update user order fail, third part order number: {orderId}", input.MerchantOrderNo);
                return new BasicOrderResult() { Message = $"Update order failed,{result.Message}" };
            }

            await _distributedEventBus.PublishAsync(_objectMapper.Map<OrderGrainDto, OrderEto>(result.Data));

            var statusInfoDto = _objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data); // for debug
            await _thirdPartOrderProvider.AddOrderStatusInfoAsync(_objectMapper.Map<OrderGrainDto, OrderStatusInfoGrainDto>(result.Data));
            
            return new BasicOrderResult() { Success = true, Message = result.Message };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Occurred error during update alchemy order.");
            throw new UserFriendlyException("Occurred error during update alchemy order.");
        }
    }

    public async Task UpdateAlchemyTxHashAsync(SendAlchemyTxHashDto input)
    {
        try
        {
            _logger.LogInformation("UpdateAlchemyTxHash OrderId: {OrderId} TxHash:{TxHash} will send to alchemy",
                input.OrderId, input.TxHash);
            Guid orderId = ThirdPartHelper.GetOrderId(input.OrderId);
            var orderData = await _thirdPartOrderProvider.GetThirdPartOrderAsync(orderId.ToString());
            if (orderData == null)
            {
                _logger.LogError("No order found for {orderId}", orderId);
                throw new UserFriendlyException($"No order found for {orderId}");
            }

            var orderPendingUpdate = _objectMapper.Map<OrderDto, WaitToSendOrderInfoDto>(orderData);
            orderPendingUpdate.TxHash = input.TxHash;
            orderPendingUpdate.AppId = _alchemyOptions.AppId;

            orderPendingUpdate.Signature = GetAlchemySignature(orderPendingUpdate.OrderNo, orderPendingUpdate.Crypto,
                orderPendingUpdate.Network, orderPendingUpdate.Address);

            await _alchemyProvider.HttpPost2AlchemyAsync(_alchemyOptions.UpdateSellOrderUri,
                JsonConvert.SerializeObject(orderPendingUpdate, Formatting.None, _setting));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Occurred error during update alchemy order transaction hash.");
        }
    }

    public async Task TransactionAsync(TransactionDto input)
    {
        VerifySignature(input);
        var transactionEto = ObjectMapper.Map<TransactionDto, TransactionEto>(input);
        await _distributedEventBus.PublishAsync(transactionEto);
    }

    private void VerifySignature(TransactionDto input)
    {
        try
        {
            var validStr = EncryptionHelper.MD5Encrypt32(input.OrderId + input.RawTransaction);
            var publicKey = ByteArrayHelper.HexStringToByteArray(input.PublicKey);
            var signature = ByteArrayHelper.HexStringToByteArray(input.Signature);
            var data = Encoding.UTF8.GetBytes(validStr).ComputeHash();
            
            if (!CryptoHelper.VerifySignature(signature, data, publicKey))
                throw new UserFriendlyException("data validation failed");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Input validation internal error");
            throw new UserFriendlyException("Something was wrong, please try again later!");
        }
    }

    private OrderGrainDto MergeEsAndInput2GrainModel(AlchemyOrderUpdateDto alchemyData, OrderDto esOrderData)
    {
        var orderGrainData = _objectMapper.Map<AlchemyOrderUpdateDto, OrderGrainDto>(alchemyData);
        var orderData = _objectMapper.Map<OrderDto, OrderGrainDto>(esOrderData);
        foreach (var prop in typeof(OrderGrainDto).GetProperties())
        {
            // When the attribute in UpdateOrderData has been assigned, there is no need to overwrite it with the data in es
            if (prop.GetValue(orderGrainData) == null && prop.GetValue(orderData) != null)
            {
                prop.SetValue(orderGrainData, prop.GetValue(orderData));
            }
        }

        return orderGrainData;
    }

    private string GetAlchemySignature(string orderNo, string crypto, string network, string address)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_alchemyOptions.AppId + _alchemyOptions.AppSecret + orderNo + crypto +
                                                  network + address);
            byte[] hashBytes = SHA1.Create().ComputeHash(bytes);

            StringBuilder sb = new StringBuilder();
            foreach (var t in hashBytes)
            {
                sb.Append(t.ToString("X2"));
            }

            _logger.LogDebug("Generate Alchemy sell order signature successfully. Signature: {signature}",
                sb.ToString().ToLower());
            return sb.ToString().ToLower();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Generator alchemy update txHash signature failed, OrderNo: {orderNo}.", orderNo);
            return "";
        }
    }
}