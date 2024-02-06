using System;
using System.Collections.Generic;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.Ramp;
using Newtonsoft.Json;

namespace CAServer.Options;

public class RampOptions
{
    public Dictionary<string, ThirdPartProvider> Providers { get; set; } = new();
    public List<string> PortkeyIdWhiteList { get; set; }
    public DefaultCurrencyOption DefaultCurrency { get; set; }
    public List<CryptoItem> CryptoList { get; set; }
    public Dictionary<string, CoverageExpression> CoverageExpressions { get; set; }

    public ThirdPartProvider Provider(ThirdPartNameType thirdPart)
    {
        return Providers.TryGetValue(thirdPart.ToString(), out var provider) ? provider : null;
    }

}

public class CoverageExpression
{
    public List<string> OnRamp { get; set; }
    public List<string> OffRamp { get; set; }
}

public class ThirdPartProvider
{
    public string AppId { get; set; }
    public string BaseUrl { get; set; }
    public string Name { get; set; }
    public string Logo { get; set; }
    public string WebhookUrl { get; set; }
    public string CountryIconUrl { get; set; }
    public List<string> PaymentTags { get; set; } = new();
    // standard-Network => thirdPart-Network
    public Dictionary<string, string> NetworkMapping { get; set; } = new();
    // standard-Symbol => thirdPart-Symbol
    public Dictionary<string, string> SymbolMapping { get; set; } = new();
    public ProviderCoverage Coverage { get; set; }
}

public class CryptoItem
{
    public string Symbol { get; set; }
    public string Icon { get; set; }
    public string Decimals { get; set; }
    public string Network { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public bool OnRampEnable { get; set; } = true;
    public bool OffRampEnable { get; set; } = true;
}

public class ProviderCoverage
{
    public bool OnRamp { get; set; }
    public bool OffRamp { get; set; }
}

public class DefaultCurrencyOption
{
    public string Crypto { get; set; } = "ELF";
    public string CryptoAmount { get; set; } = "400";
    public string Network { get; set; } = "AELF";
    public string ChainId { get; set; } = "AELF";
    public string Fiat { get; set; } = "USD";
    public string FiatAmount { get; set; } = "200";
    public string Country { get; set; } = "US";

    public DefaultCryptoCurrency ToCrypto()
    {
        return new DefaultCryptoCurrency
        {
            Symbol = Crypto,
            Amount = CryptoAmount,
            Network = Network,
            ChainId = ChainId,
        };
    }

    public DefaultFiatCurrency ToFiat()
    {
        return new DefaultFiatCurrency
        {
            Symbol = Fiat,
            Amount = FiatAmount,
            Country = Country
        };
    }
}