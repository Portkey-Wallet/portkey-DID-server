using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.IpInfo;

public class IpInfoDto
{
    public bool Success { get; set; }
    public Error Error { get; set; }
    public string Ip { get; set; }
    public string Type { get; set; }

    [JsonProperty("continent_code")] public string ContinentCode { get; set; }
    [JsonProperty("continent_name")] public string ContinentName { get; set; }
    [JsonProperty("country_code")] public string CountryCode { get; set; }
    [JsonProperty("country_name")] public string CountryName { get; set; }
    [JsonProperty("region_code")] public string RegionCode { get; set; }
    [JsonProperty("region_name")] public string RegionName { get; set; }
    public string City { get; set; }
    public string Zip { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public LocationInfo Location { get; set; }
}

public class Error
{
    public string Code { get; set; }
    public string Type { get; set; }
    public string Info { get; set; }
}

public class LocationInfo
{
    [JsonProperty("geoname_id")] public int? GeonameId { get; set; }
    public string Capital { get; set; }
    public List<LanguageInfo> Languages { get; set; }
    [JsonProperty("country_flag")] public string CountryFlag { get; set; }
    [JsonProperty("country_flag_emoji")] public string CountryFlagEmoji { get; set; }

    [JsonProperty("country_flag_emoji_unicode")]
    public string CountryFlagEmojiUnicode { get; set; }

    [JsonProperty("calling_code")] public string CallingCode { get; set; }
    [JsonProperty("is_eu")] public bool? IsEu { get; set; }
}

public class LanguageInfo
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Native { get; set; }
}