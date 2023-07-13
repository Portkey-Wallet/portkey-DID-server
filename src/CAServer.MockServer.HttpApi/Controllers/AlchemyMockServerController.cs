using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockServer.Dtos;
using MockServer.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using OrderStatusType = MockServer.Dtos.OrderStatusType;
using TransferDirectionType = MockServer.Dtos.TransferDirectionType;

namespace MockServer.Controllers;

[RemoteService]
[Route("api/app/alchemy")]
public class AlchemyMockServerController : CAServerMockServerController
{
    private readonly ILogger<AlchemyMockServerController> _logger;
    private readonly AlchemyOptions _alchemyOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IAlchemyMockServerProvider _alchemyMockServerProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly JsonSerializerSettings _setting = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public AlchemyMockServerController(ILogger<AlchemyMockServerController> logger,
        IOptionsSnapshot<AlchemyOptions> alchemyOptions,
        IDistributedEventBus distributedEventBus, IObjectMapper objectMapper,
        IHttpClientFactory httpClientFactory, IAlchemyMockServerProvider alchemyMockServerProvider)
    {
        _logger = logger;
        _alchemyOptions = alchemyOptions.Value;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _httpClientFactory = httpClientFactory;
        _alchemyMockServerProvider = alchemyMockServerProvider;
    }

    [HttpPost("/webhooks/off/merchant")]
    public async Task<AlchemyResponseDto> SendTxHashToMockAlchemyAsync(SendTxHashToMockAlchemyDto input)
    {
        var orderData = await _alchemyMockServerProvider.GetThirdPartOrderAsync(input.OrderNo);
        if (!string.IsNullOrEmpty(orderData.OrderNo) && orderData.OrderNo == input.OrderNo)
        {
            orderData.Status = AlchemyHelper.GetOrderStatus(OrderStatusType.SuccessfulPayment);
            await _distributedEventBus.PublishAsync(orderData);

            StringContent str2Json = new StringContent(JsonConvert.SerializeObject(orderData, Formatting.None,
                _setting), Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage respMsg = await client.PostAsync(_alchemyOptions.PortKeyBaseUrl +
                                                                 _alchemyOptions.PortKeyCallbackUri, str2Json);
            var respStr = await respMsg.Content.ReadAsStringAsync();
            _logger.LogInformation("[{StatusCode}]ResBody: {ResBody}", respMsg.StatusCode, respStr);
        }

        return new AlchemyResponseDto();
    }

    [HttpPut("/order")]
    public async Task<UpdateMockAlchemyOrderResponseDto> UpdateAlchemyMockOrderAsync(UpdateAlchemyMockOrderDto input)
    {
        var orderData = await _alchemyMockServerProvider.GetThirdPartOrderAsync(input.OrderNo);
        if (string.IsNullOrEmpty(orderData.OrderNo) || orderData.OrderNo != input.OrderNo)
        {
            return new UpdateMockAlchemyOrderResponseDto()
            {
                Success = "false",
                ReturnMsg = "Order updated es failed"
            };
        }

        var mergedOrderData = MergeEsAndInput(input, orderData);
        await _distributedEventBus.PublishAsync(mergedOrderData);

        return new UpdateMockAlchemyOrderResponseDto()
        {
            Data = mergedOrderData
        };
    }

    [HttpPost("/order")]
    public async Task<CreateMockAlchemyOrderResponseDto> CreateAlchemyMockOrderAsync(CreateAlchemyMockOrderDto input)
    {
        var orderData = _objectMapper.Map<CreateAlchemyMockOrderDto, AlchemyOrderDto>(input);
        orderData.OrderNo = GuidGenerator.Create().ToString();
        orderData.Side = TransferDirectionType.SELL.ToString();
        orderData.Status = AlchemyHelper.GetOrderStatus(OrderStatusType.StartPayment);

        await _distributedEventBus.PublishAsync(orderData);

        return new CreateMockAlchemyOrderResponseDto()
        {
            Data = orderData
        };
    }

    private AlchemyOrderDto MergeEsAndInput(UpdateAlchemyMockOrderDto alchemyData, AlchemyOrderDto esOrderData)
    {
        var orderGrainData = _objectMapper.Map<UpdateAlchemyMockOrderDto, AlchemyOrderDto>(alchemyData);
        foreach (var prop in typeof(AlchemyOrderDto).GetProperties())
        {
            if (prop.GetValue(orderGrainData) == null && prop.GetValue(esOrderData) != null)
            {
                prop.SetValue(orderGrainData, prop.GetValue(esOrderData));
            }
        }

        return orderGrainData;
    }
}

public static class AlchemyHelper
{
    private static Dictionary<OrderStatusType, string> _orderStatusDict = new()
    {
        { OrderStatusType.Finish, "FINISHED" },
        { OrderStatusType.Failed, "PAY_FAIL" },
        { OrderStatusType.Created, "1" },
        { OrderStatusType.StartPayment, "3" },
        { OrderStatusType.SuccessfulPayment, "4" },
        { OrderStatusType.PaymentFailed, "5" },
    };

    public static string GetOrderStatus(OrderStatusType status)
    {
        if (_orderStatusDict.TryGetValue(status, out string _))
        {
            return _orderStatusDict[status];
        }

        return "Unknown";
    }
}