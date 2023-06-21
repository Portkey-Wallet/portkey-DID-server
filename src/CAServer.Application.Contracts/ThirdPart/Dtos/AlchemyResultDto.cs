using System;
using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class AlchemyBaseResponseDto
{
    public string Success { get; set; } = "Success";
    public string ReturnCode { get; set; } = "0000";
    public string ReturnMsg { get; set; } = "";
    public string Extend { get; set; } = "";
    public string TraceId { get; set; }
}

public class AlchemyTokenDto : AlchemyBaseResponseDto
{
    public AlchemyTokenDataDto Data { get; set; }
}

public class AlchemyTokenDataDto
{
    public string Email { get; set; }
    public string AccessToken { get; set; }
}

public class AlchemyFiatListDto : AlchemyBaseResponseDto
{
    public List<AlchemyFiatDto> Data { get; set; }
}

public class AlchemyFiatDto
{
    public string Currency { get; set; }
    public string Country { get; set; }
    public string PayWayCode { get; set; }
    public string PayWayName { get; set; }
    public string FixedFee { get; set; }
    public string RateFee { get; set; }
    public string PayMin { get; set; }
    public string PayMax { get; set; }
    public string CountryName { get; set; }
}

public class AlchemyCryptoListDto : AlchemyBaseResponseDto
{
    public List<AlchemyCryptoDto> Data { get; set; }
}

public class AlchemyCryptoDto
{
    public string Crypto { get; set; }
    public string Network { get; set; }
    public string BuyEnable { get; set; }
    public string SellEnable { get; set; }
    public string MinPurchaseAmount { get; set; }
    public string MaxPurchaseAmount { get; set; }
    public string Address { get; set; }
    public string Icon { get; set; }
    public string MinSellAmount { get; set; }
    public string MaxSellAmount { get; set; }
}

public class AlchemyOrderQuoteResultDto : AlchemyBaseResponseDto
{
    public AlchemyOrderQuoteDataDto Data { get; set; }
}

public class AlchemyOrderQuoteDataDto
{
    public string Crypto { get; set; }
    public string CryptoPrice { get; set; }
    public string CryptoQuantity { get; set; }
    public string Fiat { get; set; }
    public string FiatQuantity { get; set; }
    public string RampFee { get; set; }
    public string NetworkFee { get; set; }
    public string PayWayCode { get; set; }
}

public class AlchemySignatureResultDto : AlchemyBaseResponseDto
{
    public string Signature { get; set; }
}

public class AlchemyTargetAddressDto
{
    public Guid OrderId { get; set; }
    public string MerchantName { get; set; }
    public string Address { get; set; }
    public string Network { get; set; }
    public string Crypto { get; set; }
    public string CryptoAmount { get; set; }
}