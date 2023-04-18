using System;

namespace CAServer.CAActivity.Dto;

public class GetActivitiesDto : ActivityBase
{
    public NftDetail NftInfo { get; set; }
    public string ListIcon { set; get; }
}

public class NftDetail
{
    public string ImageUrl { get; set; }
    public string Alias { get; set; }
    public string NftId { set; get; }
}