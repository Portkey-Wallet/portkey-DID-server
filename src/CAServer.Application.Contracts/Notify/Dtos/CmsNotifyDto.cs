using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CAServer.Notify.Dtos;

public class CmsNotifyDto
{
    public List<CmsNotify> Data { get; set; }
}

public class CmsNotify
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string AppId { get; set; }
    public CmsTargetVersion TargetVersion { get; set; }
    public List<CmsAppVersion> AppVersions { get; set; }
    public string DownloadUrl { get; set; }
    [JsonProperty("date_created")] public DateTime ReleaseTime { get; set; }
    public List<CmsDeviceType> DeviceTypes { get; set; }
    public List<CmsDeviceBrand> DeviceBrands { get; set; }
    public List<CmsOperatingSystemVersion> OperatingSystemVersions { get; set; }
    public List<CmsCountry> Countries { get; set; }
    public CmsStyleType StyleType { get; set; }
    public bool IsForceUpdate { get; set; }
    public bool IsApproved { get; set; }
}

public class CmsAppVersion
{
    [JsonProperty("appVersion_id")] public CmsValue<string> AppVersion { get; set; }
}

public class CmsCountry
{
    [JsonProperty("country_id")] public CmsValue<string> Country { get; set; }
}

public class CmsDeviceBrand
{
    [JsonProperty("deviceBrand_id")] public CmsValue<string> DeviceBrand { get; set; }
}

public class CmsDeviceType
{
    [JsonProperty("deviceType_id")] public CmsValue<int> DeviceType { get; set; }
}

public class CmsTargetVersion
{
    public string Value { get; set; }
}

public class CmsStyleType
{
    public int Value { get; set; }
}

public class CmsOperatingSystemVersion
{
    [JsonProperty("operatingSystemVersion_id")] public CmsValue<string> OperatingSystemVersion { get; set; }
}

public class CmsValue<T>
{
    public T Value { get; set; }
}