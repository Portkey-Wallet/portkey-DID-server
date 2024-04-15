using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Transak;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.Ramp;

public partial class ThirdPartOrderAppServiceTest
{
    private AlchemyOrderQuoteDataDto _alchemyOrderQuote = new AlchemyOrderQuoteDataDto
    {
        Crypto = "ELF",
        CryptoPrice = "0.354321",
        CryptoQuantity = "200",
        Fiat = "USD",
        FiatQuantity = "65",
        RampFee = "4.00",
        NetworkFee = "1",
        PayWayCode = "1001"
    };
    
    private readonly TransakRampPrice _transaKPriceUSD = JsonConvert.DeserializeObject<TransakRampPrice>(
        """
        {
          "conversionPrice": 2.811223,
          "fiatCurrency": "USD",
          "cryptoCurrency": "ELF",
          "paymentMethod": "credit_debit_card",
          "fiatAmount": 64,
          "cryptoAmount": 200,
          "isBuyOrSell": "BUY",
          "feeDecimal": 0.0668,
          "feeBreakdown": [
            {
              "name": "Transak fee",
              "value": 3.99,
              "id": "transak_fee",
              "ids": [
                "transak_fee"
              ]
            },
            {
              "name": "Network/Exchange fee",
              "value": 2.69,
              "id": "network_fee",
              "ids": [
                "network_fee"
              ]
            }
          ]
        }
        """
    );
    
    private readonly TransakRampPrice _transaKPriceEUR = JsonConvert.DeserializeObject<TransakRampPrice>(
        """
        {
          "conversionPrice": 2.95123,
          "fiatCurrency": "EUR",
          "cryptoCurrency": "ELF",
          "paymentMethod": "credit_debit_card",
          "fiatAmount": 65,
          "cryptoAmount": 220,
          "isBuyOrSell": "BUY",
          "feeDecimal": 0.0668,
          "feeBreakdown": [
            {
              "name": "Transak fee",
              "value": 4.99,
              "id": "transak_fee",
              "ids": [
                "transak_fee"
              ]
            },
            {
              "name": "Network/Exchange fee",
              "value": 2.69,
              "id": "network_fee",
              "ids": [
                "network_fee"
              ]
            }
          ]
        }
        """
    );
    
    
    private void MockRampPrice()
    {
        MockHttpByPath(AlchemyApi.RampOrderQuote, new AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>
        {
            Data = _alchemyOrderQuote
        });
    
        MockHttpByPath(TransakApi.GetPrice, new TransakBaseResponse<TransakRampPrice>
        {
            Response = _transaKPriceUSD
        }, """ param["fiatCurrency"] =="USD" """);
    
        MockHttpByPath(TransakApi.GetPrice, new TransakBaseResponse<TransakRampPrice>
        {
            Response = _transaKPriceEUR
        }, """ param["fiatCurrency"] =="EUR" """);
    }
    
    
    [Fact]
    public async Task RampPriceTest()
    {
        MockRampLists();
        MockRampPrice();
    
        var price = await _thirdPartOrderAppService.GetRampPriceAsync(new RampDetailRequest
        {
            Type = OrderTransDirect.SELL.ToString(),
            Crypto = "ELF",
            CryptoAmount = 200,
            Network = "AELF",
            Fiat = "USD",
            FiatAmount = 65,
            Country = "US",
        });
        price.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task ExchangeTest()
    {
        MockRampLists();
        MockRampPrice();
        
    
        var usdExchange = await _thirdPartOrderAppService.GetRampExchangeAsync(new RampExchangeRequest
        {
            Type = OrderTransDirect.SELL.ToString(),
            Crypto = "ELF",
            Network = "AELF",
            Fiat = "USD",
            Country = "US",
        });
        
        usdExchange.ShouldNotBeNull();
        usdExchange.Success.ShouldBeTrue();
        usdExchange.Data.Exchange.ShouldBe("0.355717");
    }
    
    
    [Fact]
    public async Task RampLimitTest()
    {
        MockRampLists();
        MockRampPrice();
    
        var sellLimit = await _thirdPartOrderAppService.GetRampLimitAsync(new RampLimitRequest
        {
            Type = OrderTransDirect.SELL.ToString(),
            Crypto = "ELF",
            Network = "AELF",
            Fiat = "USD",
            Country = "US",
        });
        sellLimit.ShouldNotBeNull();
        sellLimit.Success.ShouldBeTrue();
    
        var buyLimit = await _thirdPartOrderAppService.GetRampLimitAsync(new RampLimitRequest
        {
            Type = OrderTransDirect.BUY.ToString(),
            Crypto = "ELF",
            Network = "AELF",
            Fiat = "USD",
            Country = "US",
        });
        buyLimit.ShouldNotBeNull();
        buyLimit.Success.ShouldBeTrue();
    }
    
    [Fact]
    public async Task RampDetailTest()
    {
        MockRampLists();
        MockRampPrice();
    
        var buyDetail = await _thirdPartOrderAppService.GetRampDetailAsync(new RampDetailRequest
        {
            Type = OrderTransDirect.BUY.ToString(),
            Crypto = "ELF",
            CryptoAmount = 200,
            Network = "AELF",
            Fiat = "USD",
            FiatAmount = 65,
            Country = "US",
        });
        buyDetail.ShouldNotBeNull();
        buyDetail.Success.ShouldBeTrue();
    
        var sellDetail = await _thirdPartOrderAppService.GetRampDetailAsync(new RampDetailRequest
        {
            Type = OrderTransDirect.SELL.ToString(),
            Crypto = "ELF",
            CryptoAmount = 200,
            Network = "AELF",
            Fiat = "USD",
            FiatAmount = 65,
            Country = "US",
        });
        sellDetail.ShouldNotBeNull();
        sellDetail.Success.ShouldBeTrue();
    }
}