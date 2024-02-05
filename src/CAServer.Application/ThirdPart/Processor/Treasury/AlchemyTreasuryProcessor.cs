using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Signature.Provider;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.Tokens;
using CAServer.Tokens.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.ThirdPart.Processor.Treasury;

public class AlchemyTreasuryProcessor : AbstractTreasuryProcessor
{
    private readonly ILogger<AlchemyTreasuryProcessor> _logger;
    private readonly IOptionsMonitor<ThirdPartOptions> _thirdPartOptions;
    private readonly IOptionsMonitor<RampOptions> _rampOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly AlchemyProvider _alchemyProvider;
    private readonly ISecretProvider _secretProvider;


    public AlchemyTreasuryProcessor(ITokenAppService tokenAppService, IOptionsMonitor<ChainOptions> chainOptions,
        IOptionsMonitor<ThirdPartOptions> thirdPartOptions, IOptionsMonitor<RampOptions> rampOptions,
        IObjectMapper objectMapper, IClusterClient clusterClient, IThirdPartOrderProvider thirdPartOrderProvider,
        ILogger<AlchemyTreasuryProcessor> logger, ITreasuryOrderProvider treasuryOrderProvider,
        AlchemyProvider alchemyProvider, IContractProvider contractProvider, ITokenProvider tokenProvider, ISecretProvider secretProvider) :
        base(tokenAppService, chainOptions, clusterClient, objectMapper, thirdPartOptions,
            thirdPartOrderProvider, logger, treasuryOrderProvider, contractProvider, tokenProvider)
    {
        _thirdPartOptions = thirdPartOptions;
        _rampOptions = rampOptions;
        _objectMapper = objectMapper;
        _logger = logger;
        _alchemyProvider = alchemyProvider;
        _secretProvider = secretProvider;
    }

    public override ThirdPartNameType ThirdPartName()
    {
        return ThirdPartNameType.Alchemy;
    }

    public string MappingToAlchemyNetwork(string network)
    {
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .TryGetValue(network, out var mappingNetwork);
        return mappingExists ? mappingNetwork : network;
    }

    public string MappingFromAlchemyNetwork(string network)
    {
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).NetworkMapping
            .FirstOrDefault(kv => kv.Value == network);
        return mappingNetwork.Key.DefaultIfEmpty(network);
    }

    public string MappingToAlchemySymbol(string symbol)
    {
        var mappingExists = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .TryGetValue(symbol, out var achSymbol);
        return mappingExists ? achSymbol : symbol;
    }

    public string MappingFromAchSymbol(string achSymbol)
    {
        var mappingNetwork = _rampOptions.CurrentValue.Provider(ThirdPartNameType.Alchemy).SymbolMapping
            .FirstOrDefault(kv => kv.Value == achSymbol);
        return mappingNetwork.Key.DefaultIfEmpty(achSymbol);
    }

    public override async Task<Tuple<bool, string>> CallBackThirdPartAsync(TreasuryOrderDto orderDto)
    {
        try
        {
            var totalFeeInUsdt = orderDto.FeeInfo
                .Select(fee => fee.Amount.SafeToDecimal() * fee.SymbolPriceInUsdt.SafeToDecimal())
                .Sum();
            var cryptoExchange = orderDto.TokenExchanges.FirstOrDefault(ex =>
                ex.FromSymbol == orderDto.Crypto && ex.ToSymbol == CommonConstant.USDT);
            AssertHelper.NotNull(cryptoExchange, "crypto exchange notfound of {}-USDT", orderDto.Crypto);
            var totalFeeInCrypto = totalFeeInUsdt * cryptoExchange!.Exchange;
            var response = await _alchemyProvider.CallBackTreasuryOrder(new AlchemyTreasuryCallBackDto
            {
                OrderNo = orderDto.ThirdPartOrderId,
                Crypto = orderDto.ThirdPartCrypto,
                CryptoAmount = orderDto.CryptoAmount.ToString(CultureInfo.InvariantCulture),
                CryptoPrice = orderDto.CryptoPriceInUsdt.ToString(CultureInfo.InvariantCulture),
                TxHash = orderDto.TransactionId,
                Network = orderDto.ThirdPartNetwork,
                Address = orderDto.ToAddress,
                NetworkFee = totalFeeInCrypto.ToString(8, DecimalHelper.RoundingOption.Ceiling),
            });
            return Tuple.Create(response.Success, JsonConvert.SerializeObject(response));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Alchemy treasury callback error");
            return Tuple.Create(false, e.Message);
        }
    }

    internal override async Task<string> AdaptPriceInputAsync<TPriceInput>(TPriceInput priceInput)
    {
        AssertHelper.IsTrue(priceInput is AlchemyTreasuryPriceRequestDto,
            "Treasury price input not AlchemyTreasuryPriceRequestDto");
        var input = priceInput as AlchemyTreasuryPriceRequestDto;
        await AssertSignatureAsync(input);

        return MappingFromAchSymbol(input!.Crypto);
    }

    internal override Task<TreasuryBaseResult> AdaptPriceOutputAsync(
        TreasuryPriceDto treasuryPriceDto)
    {
        var networkList = new List<AlchemyTreasuryPriceResultDto.AlchemyTreasuryNetwork>();
        foreach (var (network, fee) in treasuryPriceDto.NetworkFee)
        {
            var price = fee.Amount.SafeToDecimal() * fee.SymbolPriceInUsdt.SafeToDecimal();
            networkList.Add(new AlchemyTreasuryPriceResultDto.AlchemyTreasuryNetwork
            {
                Network = MappingToAlchemyNetwork(network),
                NetworkFee = price.ToString(CultureInfo.InvariantCulture)
            });
        }

        return Task.FromResult<TreasuryBaseResult>(new AlchemyTreasuryPriceResultDto
        {
            Price = treasuryPriceDto.Price.ToString(CultureInfo.InvariantCulture),
            NetworkList = networkList
        });
    }

    internal override async Task<TreasuryOrderRequest> AdaptOrderInputAsync<TOrderInput>(TOrderInput orderInput)
    {
        AssertHelper.IsTrue(orderInput is AlchemyTreasuryOrderRequestDto,
            "Treasury order input not AlchemyTreasuryOrderRequestDto");
        var input = orderInput as AlchemyTreasuryOrderRequestDto;
        await AssertSignatureAsync(input);

        var standardNetwork = MappingFromAlchemyNetwork(input!.Network);
        var standardCrypto = MappingFromAchSymbol(input.Crypto);
        AssertHelper.NotEmpty(standardNetwork, "Input network not support {}", input.Network);
        AssertHelper.IsTrue(_rampOptions.CurrentValue.CryptoList.Any(crypto =>
                crypto.OnRampEnable && crypto.Network == standardNetwork && crypto.Symbol == standardCrypto),
            "Symbol not support {} of network {}", standardCrypto, standardNetwork);

        var orderRequest = _objectMapper.Map<AlchemyTreasuryOrderRequestDto, TreasuryOrderRequest>(input);
        orderRequest.ThirdPartName = ThirdPartName().ToString();
        orderRequest.Network = standardNetwork;
        orderRequest.Crypto = standardCrypto;
        orderRequest.ThirdPartCrypto = input.Crypto;
        orderRequest.ThirdPartNetwork = input.Network;
        return orderRequest;
    }

    private async Task AssertSignatureAsync(TreasuryBaseContext treasuryBaseContext)
    {
        AssertHelper.NotNull(treasuryBaseContext.Headers, "Http context empty");

        var headers = treasuryBaseContext.Headers;
        AssertHelper.NotEmpty(headers, "Http header empty");
        AssertHelper.IsTrue(headers!.TryGetValue("appid", out var appId), "AppId header required");
        AssertHelper.IsTrue(headers.TryGetValue("timestamp", out var timestamp), "Timestamp header required");
        AssertHelper.IsTrue(headers.TryGetValue("sign", out var sign), "Sign header required");
        AssertHelper.IsTrue(appId! == _thirdPartOptions.CurrentValue.Alchemy.AppId, "AppId not match");

        var minTs = DateTime.UtcNow.AddSeconds( - _thirdPartOptions.CurrentValue.Alchemy.TimestampExpireSeconds)
            .ToUtcMilliSeconds();
        var maxTs = DateTime.UtcNow.AddSeconds(_thirdPartOptions.CurrentValue.Alchemy.TimestampExpireSeconds)
            .ToUtcMilliSeconds();
        var ts = timestamp.SafeToLong();
        AssertHelper.IsTrue(ts >= minTs && ts <= maxTs, "Invalid timestamp");

        var expectedSign = await _secretProvider.GetAlchemyShaSignAsync(appId, timestamp!);
        AssertHelper.IsTrue(expectedSign == sign, "Invalid signature");
    }
}