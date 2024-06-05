using System.Collections.Generic;
using CAServer.CAActivity.Dtos;

namespace CAServer.CAActivity.Dto;

public class GetActivitiesDto
{
    public List<GetActivityDto> Data { get; set; }
    public bool HasNextPage { get; set; }
    public long TotalRecordCount { get; set; }
}

public class GetActivityDto : ActivityBase
{
    public NftDetail NftInfo { get; set; }
    public string ListIcon { set; get; }
    public string CurrentPriceInUsd { get; set; }
    public string CurrentTxPriceInUsd { get; set; }
    public string DappName { get; set; }
    public string DappIcon { get; set; }
    public List<OperationItemInfo> Operations { get; set; } = new();
}

public class NftDetail
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string NftId { get; set; }
    public bool IsSeed { get; set; }
    public int SeedType { get; set; }
}

public class OperationItemInfo
{
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Decimals { get; set; }
    public string Icon { get; set; }
    public bool IsReceived { get; set; }
    public NftDetail NftInfo { get; set; }
}