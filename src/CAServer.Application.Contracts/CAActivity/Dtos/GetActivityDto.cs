using System.Collections.Generic;
using CAServer.CAActivity.Dtos;
using Orleans;

namespace CAServer.CAActivity.Dto;

public class GetActivitiesDto
{
    public List<GetActivityDto> Data { get; set; }
    public bool HasNextPage { get; set; }
    public long TotalRecordCount { get; set; }
}

[GenerateSerializer]
public class GetActivityDto : ActivityBase
{
    [Id(0)]
    public NftDetail NftInfo { get; set; }
    [Id(1)]
    public string ListIcon { set; get; }
    [Id(2)]
    public string CurrentPriceInUsd { get; set; }
    [Id(3)]
    public string CurrentTxPriceInUsd { get; set; }
    [Id(4)]
    public string DappName { get; set; }
    [Id(5)]
    public string DappIcon { get; set; }
    [Id(6)]
    public List<OperationItemInfo> Operations { get; set; } = new();
}

[GenerateSerializer]
public class NftDetail
{
    [Id(0)]
    public string ImageUrl { get; set; }
    [Id(1)]
    public string Alias { get; set; }
    [Id(2)]
    public string NftId { get; set; }
    [Id(3)]
    public bool IsSeed { get; set; }
    [Id(4)]
    public int SeedType { get; set; }
}


[GenerateSerializer]
public class OperationItemInfo
{
    [Id(0)]
    public string Symbol { get; set; }
    [Id(1)]
    public string Amount { get; set; }
    [Id(2)]
    public string Decimals { get; set; }
    [Id(3)]
    public string Icon { get; set; }
    [Id(4)]
    public bool IsReceived { get; set; }
    [Id(5)]
    public NftDetail NftInfo { get; set; }
}
