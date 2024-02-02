using System.Collections.Generic;
using CAServer.Commons;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart.Dtos.ThirdPart;

public class AlchemyBaseResponseDto<T>
{
    public const string SuccessCode = "0000";

    public bool Success => ReturnCode == SuccessCode;
    public string ReturnCode { get; set; } = SuccessCode;
    public string ReturnMsg { get; set; } = "SUCCESS";
    public string Extend { get; set; } = "";
    public string TraceId { get; set; }

    public T Data { get; set; }

    public AlchemyBaseResponseDto()
    {
        
    }
    
    public AlchemyBaseResponseDto(T data)
    {
        Data = data;
    }

    public static AlchemyBaseResponseDto<T> Convert(CommonResponseDto<T> resp)
    {
        return resp.Success ? new AlchemyBaseResponseDto<T>(resp.Data) : Fail(resp.Message);
    }
    
    public static AlchemyBaseResponseDto<T> Fail(string msg, int code = 50000)
    {
        return new AlchemyBaseResponseDto<T>
        {
            ReturnMsg = msg,
            ReturnCode = code.ToString()
        };
    }

}

public class AlchemyTokenDataDto
{
    public string Email { get; set; }
    public string AccessToken { get; set; }
}

public class AlchemyFiatDto
{
    public string Currency { get; set; }
    public string Country { get; set; }
    public string PayWayCode { get; set; }
    public string PayWayName { get; set; }
    public string FixedFee { get; set; }
    public string FeeRate { get; set; }
    public string PayMin { get; set; }
    public string PayMax { get; set; }
    public string CountryName { get; set; }
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
    
    // not from ach
    public string Network { get; set; }

    public string CryptoNetworkFee { get; set; }
}

public class AlchemySignatureResultDto : AlchemyBaseResponseDto<Empty>
{
    public string Signature { get; set; }
}

public class AlchemyTreasuryPriceResultDto : TreasuryBaseResult
{
    public string Price { get; set; }

    public List<AlchemyTreasuryNetwork> NetworkList { get; set; }
    
    public class AlchemyTreasuryNetwork
    {
        public string Network { get; set; }
        public string NetworkFee { get; set; }
    }
    
}

public class AlchemyTreasuryCallBackDto
{
    public string OrderNo {get;set;}
    public string Crypto {get;set;}
    public string CryptoAmount {get;set;}
    public string CryptoPrice {get;set;}
    public string TxHash {get;set;}
    public string Network {get;set;}
    public string NetworkFee {get;set;}
    public string Address {get;set;}
    public string Status {get;set;}
}