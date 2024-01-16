using System.Collections.Generic;
using CAServer.Guardian;
using CAServer.Hubs;
using CAServer.IpInfo;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.Verifier;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class ModelTest
{
    [Fact]
    public void GuardianDtoTest()
    {
        var dto = new ManagerInfoDBase
        {
            Address = string.Empty,
            ExtraData = string.Empty,
        };

        var list = new GuardianBaseListDto
        {
            Guardians = new List<GuardianInfoBase>()
        };

        var guardian = new GuardianDto
        {
            ThirdPartyEmail = string.Empty,
            IsPrivate = false,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        var baseList = new GuardianBaseListDto
        {
            Guardians = new List<GuardianInfoBase>()
        };

        dto.ShouldNotBeNull();
    }

    [Fact]
    public void HubRequestTest()
    {
        var hub = new HubRequestBase
        {
            Context = new HubRequestContext()
            {
                ClientId = "test",
                RequestId = "test"
            }
        };

        var hubPing = new HubPingRequestDto()
        {
            Content = "test"
        };

        var info = hub.Context.ToString();
        info.ShouldNotBeNull();
    }

    [Fact]
    public void IpInfoTest()
    {
        var dto = new IpInfoDto
        {
            Ip = string.Empty,
            Type = string.Empty,
            ContinentCode = string.Empty,
            ContinentName = string.Empty,
            CountryCode = string.Empty,
            CountryName = string.Empty,
            RegionCode = string.Empty,
            RegionName = string.Empty,
            City = string.Empty,
            Zip = string.Empty,
            Latitude = 0,
            Longitude = 0,
            Location = new LocationInfo()
            {
                GeonameId = 1,
                Capital = string.Empty,
                Languages = new List<LanguageInfo>()
                {
                    new LanguageInfo()
                    {
                        Code = string.Empty,
                        Name = string.Empty,
                        Native = string.Empty
                    }
                },
                CountryFlag = string.Empty,
                CountryFlagEmoji = string.Empty,
                CountryFlagEmojiUnicode = string.Empty,
                CallingCode = string.Empty,
                IsEu = false
            }
        };

        dto.ShouldNotBeNull();
    }

    [Fact]
    public void ACHDtoTest()
    {
        var requestDto = new GetAlchemyOrderQuoteDto
        {
            Crypto = string.Empty,
            Network = string.Empty,
            Fiat = string.Empty,
            Country = string.Empty,
            Amount = string.Empty,
            Side = string.Empty,
            PayWayCode = string.Empty,
            Type = string.Empty
        };

        var responseDto = new AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>()
        {
            Data = new AlchemyOrderQuoteDataDto()
            {
                Crypto = string.Empty,
                CryptoPrice = string.Empty,
                Fiat = string.Empty,
                CryptoQuantity = string.Empty,
                RampFee = string.Empty,
                NetworkFee = string.Empty,
                PayWayCode = string.Empty
            }
        };

        var dtos = new AlchemyBaseResponseDto<List<AlchemyCryptoDto>>()
        {
            Data = new List<AlchemyCryptoDto>()
            {
                new AlchemyCryptoDto()
                {
                    Crypto = string.Empty,
                    Network = string.Empty,
                    BuyEnable = string.Empty,
                    SellEnable = string.Empty,
                    MinPurchaseAmount = string.Empty,
                    MaxPurchaseAmount = string.Empty,
                    Address = string.Empty,
                    Icon = string.Empty,
                    MinSellAmount = string.Empty,
                    MaxSellAmount = string.Empty
                }
            }
        };

        var fiatListDto = new AlchemyBaseResponseDto<List<AlchemyFiatDto>>()
        {
            Data = new List<AlchemyFiatDto>()
            {
                new AlchemyFiatDto()
                {
                    Currency = string.Empty,
                    Country = string.Empty,
                    PayWayName = string.Empty,
                    FixedFee = string.Empty,
                    FeeRate = string.Empty,
                    PayMin = string.Empty,
                    PayMax = string.Empty,
                    PayWayCode = string.Empty
                }
            }
        };

        var tokenDto = new AlchemyBaseResponseDto<AlchemyTokenDataDto>()
        {
            Data = new AlchemyTokenDataDto()
            {
                Email = string.Empty,
                AccessToken = string.Empty
            }
        };

        requestDto.ShouldNotBeNull();
    }

    [Fact]
    public void VerifierDtoTest()
    {
        var serverInfo = new GuardianVerifierServer
        {
            Name = string.Empty,
            VerifierAddress = null,
            ImageUrl = string.Empty,
            EndPoints = null
        };

        var verifierServerInfo = new VerifierServerInfo
        {
            EndPoints = new List<string>(),
            Id = string.Empty
        };

        serverInfo.ShouldNotBeNull();
    }
}