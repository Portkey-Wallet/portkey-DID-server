using System;
using System.Collections.Generic;
using CAServer.CAActivity.Dtos;

namespace CAServer.CAActivity.Dto;

public class GetActivitiesDto
{
    public List<GetActivityDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class GetActivityDto : ActivityBase
{
    public NftDetail NftInfo { get; set; }
    public string ListIcon { set; get; }
    public string CurrentPriceInUsd { get; set; }
    public string CurrentTxPriceInUsd { get; set; }
}

public class NftDetail
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string NftId { set; get; }
    public bool IsSeed { get; set; }
    public int SeedType { get; set; }
}