using System.Threading.Tasks;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Transak;
using Newtonsoft.Json;
using Xunit;

namespace CAServer.ThirdPart.Ramp;

public partial class ThirdPartOrderAppServiceTest
{

    private AlchemyOrderQuoteDataDto _alchemyOrderQuote = new AlchemyOrderQuoteDataDto
    {
        Crypto = "ELF",
        CryptoPrice = "200",
        CryptoQuantity = "20000000000",
        Fiat = "USD",
        FiatQuantity = "65",
        RampFee = "4.00",
        NetworkFee = "1",
        PayWayCode = "1001"
    };

    private TransakRampPrice _transaKPriceUSD = JsonConvert.DeserializeObject<TransakRampPrice>(
        """
        {
          "conversionPrice": 0.0005510110357006733,
          "fiatCurrency": "USD",
          "cryptoCurrency": "ELF",
          "paymentMethod": "credit_debit_card",
          "fiatAmount": 65,
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
    
    private TransakRampPrice _transaKPriceEUR = JsonConvert.DeserializeObject<TransakRampPrice>(
        """
        {
          "conversionPrice": 0.0005510110357006733,
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
    
    
    [Fact]
    public async Task RampPriceTest()
    {
        MockRampLists();
        
        MockHttpByPath(AlchemyApi.RampOrderQuote, new AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>
        {
            Data = _alchemyOrderQuote
        });
        
        MockHttpByPath(TransakApi.GetPrice, new TransakBaseResponse<TransakRampPrice>
        {
            Response = _transaKPriceUSD
        }, "param.FiatCurrency==\"USD\" ");
        
        MockHttpByPath(TransakApi.GetPrice, new TransakBaseResponse<TransakRampPrice>
        {
            Response = _transaKPriceEUR
        }, "param.FiatCurrency==\"EUR\" ");
        
        
        await _thirdPartOrderAppService.GetRampPriceAsync(new RampDetailRequest()
        {
            Type = OrderTransDirect.BUY.ToString(),
            Crypto = "ELF",
            Fiat = "ERU",
            Country = "DE",
        });
    }
    
    
    
}