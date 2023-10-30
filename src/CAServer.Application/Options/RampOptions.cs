using System.Collections.Generic;
using CAServer.ThirdPart.Dtos.Ramp;
using Newtonsoft.Json;

namespace CAServer.Options;

public class RampOptions
{
    private Dictionary<string, ThirdPartProviders> _providers = new();
    
    // accessor for _providers
    public Dictionary<string, ThirdPartProviders> Providers 
    { 
        get 
        { 
            // copy a new dict for update
            var serialized = JsonConvert.SerializeObject(_providers);
            return JsonConvert.DeserializeObject<Dictionary<string, ThirdPartProviders>>(serialized);
        }
        set => _providers = value;
    }
    public List<string> PortkeyIdWhiteList { get; set; }
    public DefaultCurrencyOption DefaultCurrency { get; set; }
    public List<CryptoItem> CryptoList { get; set; }
    
    public Dictionary<string, CoverageExpression> CoverageExpressions { get; set; }
}

public class CoverageExpression
{
    public List<string> OnRamp { get; set; }
    public List<string> OffRamp { get; set; }
}

public class ThirdPartProviders
{
    public string AppId { get; set; }
    public string BaseUrl { get; set; }
    public string Name { get; set; }
    public string Logo { get; set; }
    public string CountryIconUrl { get; set; }
    public List<string> PaymentTags { get; set; }
    public ProviderCoverage Coverage { get; set; }
}

public class CryptoItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
    public string Decimals { get; set; }
    public string Network { get; set; }
    public string Address { get; set; }
}

public class ProviderCoverage
{
    public bool OnRamp { get; set; }
    public bool OffRamp { get; set; }
}

public class DefaultCurrencyOption
{
    public string Crypto { get; set; } = "ELF";
    public string CryptoAmount { get; set; } = "40000000000";
    public string Fiat { get; set; } = "USD";
    public string FiatAmount { get; set; } = "200";

    public DefaultCurrency ToCrypto()
    {
        return new DefaultCurrency
        {
            Symbol = Crypto,
            Amount = CryptoAmount
        };
    }

    public DefaultCurrency ToFiat()
    {
        return new DefaultCurrency
        {
            Symbol = Fiat,
            Amount = FiatAmount
        };
    }
}