using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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
    private readonly AlchemyFiatDto _alchemyUsd = JsonConvert.DeserializeObject<AlchemyFiatDto>(
        """
        {
            "currency": "USD",
            "country": "US",
            "payWayCode": "10001",
            "payWayName": "Credit Card",
            "fixedFee": "0.400000",
            "feeRate": "0.039900",
            "payMin": "15.000000",
            "payMax": "2000.000000",
            "countryName": "United States of America"
        }
        """);
    
    private readonly AlchemyCryptoDto _alchemyElf = JsonConvert.DeserializeObject<AlchemyCryptoDto>(
        """
        {
        	"crypto": "ELF",
        	"network": "aelf",
        	"buyEnable": "1",
        	"sellEnable": "1",
        	"minPurchaseAmount": "15.75",
        	"maxPurchaseAmount": "1900.00",
        	"address": null,
        	"icon": "https://static.alchemypay.org/alchemypay/crypto-images/ELF.png",
        	"minSellAmount": "78.318571463",
        	"maxSellAmount": "16051.747216122"
        }
        """
    );
    
    private readonly AlchemyCryptoDto _alchemyUSDT = JsonConvert.DeserializeObject<AlchemyCryptoDto>(
        """
        {
        	"crypto": "USDT-aelf",
        	"network": "ELF",
        	"buyEnable": "1",
        	"sellEnable": "1",
        	"minPurchaseAmount": "15.75",
        	"maxPurchaseAmount": "1900.00",
        	"address": null,
        	"icon": "https://portkey-im-dev.s3.ap-northeast-1.amazonaws.com/USDT.jpg",
        	"minSellAmount": "78.318571463",
        	"maxSellAmount": "16051.747216122"
        }
        """
    );
    
    private readonly TransakFiatItem _transakUsd = JsonConvert.DeserializeObject<TransakFiatItem>(
        """
        {
          "symbol": "USD",
          "supportingCountries": [
            "US", "USC"
          ],
          "logoSymbol": "US",
          "name": "US Dollar",
          "paymentOptions": [
            {
                "name": "Card Payment",
                "id": "credit_debit_card",
                "isNftAllowed": true,
                "isNonCustodial": true,
                "processingTime": "1-3 minutes",
                "displayText": true,
                "icon": "https://assets.transak.com/images/fiat-currency/visa_master_h.png",
                "dailyLimit": 5000,
                "limitCurrency": "USD",
                "isActive": true,
                "provider": "checkout",
                "maxAmount": 1500,
                "minAmount": 30,
                "defaultAmount": 300,
                "isConverted": true,
                "isPayOutAllowed": true,
                "minAmountForPayOut": 40,
                "maxAmountForPayOut": 1600,
                "defaultAmountForPayOut": 400
            }
          ],
          "isPopular": true,
          "isAllowed": true,
          "roundOff": 2,
          "icon": "",
          "isPayOutAllowed": true,
          "defaultCountryForNFT": "US"
        }
        """
    );
    
    private readonly TransakFiatItem _transakEur = JsonConvert.DeserializeObject<TransakFiatItem>(
        """
        {
          "symbol": "EUR",
          "supportingCountries": [
            "DE","DK"
          ],
          "logoSymbol": "EU",
          "name": "EUR Dollar",
          "paymentOptions": [
            {
              "name": "Card Payment",
              "id": "credit_debit_card",
              "isNftAllowed": true,
              "processingTime": "1-3 minutes",
              "displayText": true,
              "icon": "https://assets.transak.com/images/fiat-currency/visa_master_h.png",
              "dailyLimit": 12753,
              "limitCurrency": "USD",
              "maxAmount": 2733,
              "isActive": true,
              "provider": "checkout",
              "minAmount": 27,
              "defaultAmount": 1366,
              "isConverted": true,
              "isPayOutAllowed": true,
              "minAmountForPayOut": 46,
              "maxAmountForPayOut": 4555,
              "defaultAmountForPayOut": 455
            }
          ],
          "isPopular": true,
          "isAllowed": true,
          "roundOff": 2,
          "icon": "",
          "isPayOutAllowed": true,
          "defaultCountryForNFT": "DE"
        }
        """
    );
    
    private readonly TransakCountry _transakCountryUs = JsonConvert.DeserializeObject<TransakCountry>(
        """
        {
           "alpha2": "US",
           "alpha3": "USA",
           "isAllowed": true,
           "isLightKycAllowed": false,
           "name": "United States of America"
        }
        """
    );
    
    private readonly TransakCryptoItem _transakCryptoElf = JsonConvert.DeserializeObject<TransakCryptoItem>(
        """
        {
          "coinId": "usd-coin",
          "decimals": 8,
          "image": {
            "large": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_large.png",
            "small": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_small.png",
            "thumb": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_thumb.png"
          },
          "isAllowed": true,
          "isPopular": false,
          "isStable": true,
          "name": "USD Coin",
          "roundOff": 2,
          "symbol": "ELF",
          "network": {
            "name": "aelf",
            "fiatCurrenciesNotSupported": [
              {
                "fiatCurrency": "HKD",
                "paymentMethod": "credit_debit_card"
              }
            ],
            "chainId": null
          },
          "uniqueId": "ETh",
          "isPayInAllowed": true,
          "minAmountForPayIn": 1,
          "maxAmountForPayIn": 10
        }
        """);
    
    
    
    private readonly TransakCryptoItem _transakCryptoUsdt = JsonConvert.DeserializeObject<TransakCryptoItem>(
        """
        {
          "coinId": "usdt-coin",
          "decimals": 8,
          "image": {
            "large": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_large.png",
            "small": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_small.png",
            "thumb": "https://assets-stg.transak.com/images/cryptoCurrency/usd-coin_thumb.png"
          },
          "isAllowed": true,
          "isPopular": false,
          "isStable": true,
          "name": "USDT Coin",
          "roundOff": 2,
          "symbol": "USDT",
          "network": {
            "name": "ethereum",
            "fiatCurrenciesNotSupported": [
              {
                "fiatCurrency": "HKD",
                "paymentMethod": "credit_debit_card"
              }
            ],
            "chainId": null
          },
          "uniqueId": "ETh",
          "isPayInAllowed": true,
          "minAmountForPayIn": 1,
          "maxAmountForPayIn": 10
        }
        """);
    
    
    private void MockRampLists() {
         
        MockHttpByPath(AlchemyApi.QueryFiatList, new AlchemyBaseResponseDto<List<AlchemyFiatDto>>
        {
            Data = new List<AlchemyFiatDto> { _alchemyUsd }
        });
    
        MockHttpByPath(AlchemyApi.QueryCryptoList, new AlchemyBaseResponseDto<List<AlchemyCryptoDto>>
        {
            Data = new List<AlchemyCryptoDto> { _alchemyElf, _alchemyUSDT }
        });
        
        MockHttpByPath(TransakApi.GetFiatCurrencies, new TransakBaseResponse<List<TransakFiatItem>>
        {
            Response = new List<TransakFiatItem> { _transakUsd, _transakEur }
        });
        
        MockHttpByPath(TransakApi.GetCountries,  new TransakBaseResponse<List<TransakCountry>>
        {
            Response = new List<TransakCountry> { _transakCountryUs }
        });
    
        MockHttpByPath(TransakApi.GetCryptoCurrencies, new TransakBaseResponse<List<TransakCryptoItem>>
        {
            Response = new List<TransakCryptoItem> { _transakCryptoElf, _transakCryptoUsdt } 
        });
        DeviceInfoContext.CurrentDeviceInfo = new DeviceInfo()
        {
            ClientType = "WebSDK"
        };
    }
    
    
    [Fact]
    public async Task RampFiatTest()
    {
        MockRampLists(); 
        
        var buyFiatList = await _thirdPartOrderAppService.GetRampFiatListAsync(new RampFiatRequest
        {
            Type = OrderTransDirect.BUY.ToString(),
            Crypto = "ELF",
            Network = "AELF"
        });
        
        _output.WriteLine(JsonConvert.SerializeObject(buyFiatList));
        buyFiatList.ShouldNotBeNull();
        buyFiatList.Success.ShouldBe(true);
        buyFiatList.Data.FiatList.Count.ShouldBe(4);
        
        var sellFiatList = await _thirdPartOrderAppService.GetRampFiatListAsync(new RampFiatRequest
        {
            Type = OrderTransDirect.SELL.ToString(),
        });
        
        _output.WriteLine(JsonConvert.SerializeObject(sellFiatList));
        sellFiatList.ShouldNotBeNull();
        sellFiatList.Success.ShouldBe(true);
        sellFiatList.Data.FiatList.Count.ShouldBe(4);
        
    }
    
    [Fact]
    public async Task RampCryptoTest()
    {
        MockRampLists();
        
        var cryptoList = await _thirdPartOrderAppService.GetRampCryptoListAsync(new RampCryptoRequest
        {
            Type = OrderTransDirect.BUY.ToString(),
            Fiat = "USD"
        });
    
        cryptoList.ShouldNotBeNull();
        cryptoList.Data.CryptoList.Count.ShouldBeGreaterThan(0);
    
        
    }
}