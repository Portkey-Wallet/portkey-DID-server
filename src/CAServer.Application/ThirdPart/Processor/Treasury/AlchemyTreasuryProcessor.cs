using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.Tokens;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processor.Treasury;

public class AlchemyTreasuryProcessor : AbstractTreasuryProcessor
{
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IObjectMapper _objectMapper;


    public AlchemyTreasuryProcessor(ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, IOptionsMonitor<RampOptions> rampOptions,
        IObjectMapper objectMapper, IClusterClient clusterClient, IDistributedEventBus distributedEventBus) :
        base(tokenAppService, chainOptions, clusterClient, objectMapper, thirdPartOptions, distributedEventBus)
    {
        _tokenAppService = tokenAppService;
        _thirdPartOptions = thirdPartOptions;
        _rampOptions = rampOptions;
        _objectMapper = objectMapper;
    }

    public override ThirdPartNameType ThirdPartName()
    {
        return ThirdPartNameType.Alchemy;
    }

    internal override async Task<string> AdaptPriceInputAsync<TPriceInput>(TPriceInput priceInput)
    {
        AssertHelper.IsTrue(priceInput is AlchemyTreasuryPriceRequestDto,
            "Treasury price input not AlchemyTreasuryPriceRequestDto");
        var input = priceInput as AlchemyTreasuryPriceRequestDto;
        await AssertSignatureAsync(input);
        return input!.Crypto;
    }

    internal override async Task<TreasuryBaseResult> AdaptPriceOutputAsync(
        TreasuryPriceDto treasuryPriceDto)
    {
        var exchangePrice = async (string fromSymbol, string toSymbol, decimal price) =>
        {
            if (fromSymbol == toSymbol) return price;
            var exchange = await _tokenAppService.GetAvgLatestExchangeAsync(fromSymbol, toSymbol);
            AssertHelper.NotNull(exchange, "Exchange from {} to {} not found", fromSymbol, toSymbol);
            return price * exchange.Exchange;
        };

        var networkList = new List<AlchemyTreasuryPriceResultDto.AlchemyTreasuryNetwork>();
        foreach (var (network, fee) in treasuryPriceDto.NetworkFee)
        {
            var cryptoPrice = await exchangePrice(fee.Symbol, treasuryPriceDto.Crypto, fee.Amount.SafeToDecimal());
            networkList.Add(new AlchemyTreasuryPriceResultDto.AlchemyTreasuryNetwork
            {
                Network = network,
                NetworkFee = cryptoPrice.ToString(CultureInfo.InvariantCulture)
            });
        }

        return new AlchemyTreasuryPriceResultDto
        {
            Price = treasuryPriceDto.Price.ToString(CultureInfo.InvariantCulture),
            NetworkList = networkList
        };
    }

    internal override async Task<TreasuryOrderRequest> AdaptOrderInputAsync<TOrderInput>(TOrderInput orderInput)
    {
        AssertHelper.IsTrue(orderInput is AlchemyTreasuryOrderRequestDto,
            "Treasury order input not AlchemyTreasuryOrderRequestDto");
        var input = orderInput as AlchemyTreasuryOrderRequestDto;
        await AssertSignatureAsync(input);

        var rampConfig = _rampOptions.CurrentValue.Provider(ThirdPartName());
        var mappingNetwork = rampConfig.NetworkMapping.Where(kv => kv.Value == input.Network).Select(kv => kv.Key)
            .FirstOrDefault();
        AssertHelper.NotEmpty(mappingNetwork, "Input network not support {}", input.Network);
        AssertHelper.IsTrue(_rampOptions.CurrentValue.CryptoList.Any(crypto =>
                crypto.Network == input.Network && crypto.Symbol == input.Crypto),
            "Symbol not support {} of network {}", input.Crypto, input.Network);

        var orderRequest = _objectMapper.Map<AlchemyTreasuryOrderRequestDto, TreasuryOrderRequest>(input);
        orderRequest.ThirdPartName = ThirdPartName().ToString();
        input.Network = mappingNetwork;
        return orderRequest;
    }

    private async Task AssertSignatureAsync(TreasuryBaseContext treasuryBaseContext)
    {
        AssertHelper.NotNull(treasuryBaseContext.HttpContext, "Http context empty");

        var headers = treasuryBaseContext.HttpContext!.Request.Headers;
        AssertHelper.NotEmpty(headers, "Http header empty");
        AssertHelper.IsTrue(headers.TryGetValue("appId", out var appId), "AppId header required");
        AssertHelper.IsTrue(headers.TryGetValue("timestamp", out var timestamp), "Timestamp header required");
        AssertHelper.IsTrue(headers.TryGetValue("sign", out var sign), "Sign header required");
        AssertHelper.IsTrue(appId == _thirdPartOptions.CurrentValue.Alchemy.AppId, "AppId not match");

        var minTs = DateTime.UtcNow.AddSeconds(-_thirdPartOptions.CurrentValue.Alchemy.TimestampExpireSeconds)
            .ToUtcMilliSeconds();
        var maxTs = DateTime.UtcNow.AddSeconds(_thirdPartOptions.CurrentValue.Alchemy.TimestampExpireSeconds)
            .ToUtcMilliSeconds();
        var ts = timestamp.ToString().SafeToLong();
        AssertHelper.IsTrue(ts >= minTs && ts <= maxTs, "Invalid timestamp");

        var signSource = appId + _thirdPartOptions.CurrentValue.Alchemy.AppSecret + timestamp;
        var expectedSign = AlchemyHelper.GenerateAlchemyApiSign(signSource);
        AssertHelper.IsTrue(expectedSign == sign, "Invalid signature");
    }
}