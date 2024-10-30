using System.Collections.Generic;
using CAServer.Commons.Etos;
using Volo.Abp.Application.Dtos;

namespace CAServer.UserAssets.Dtos;

public class GetNftItemInfosDto : PagedResultRequestDto
{
    public List<GetNftItemInfo> GetNftItemInfos { get; set; }
}

public class GetNftItemInfo : ChainDisplayNameDto
{
    public string CollectionSymbol { get; set; }
    public string Symbol { get; set; }
}