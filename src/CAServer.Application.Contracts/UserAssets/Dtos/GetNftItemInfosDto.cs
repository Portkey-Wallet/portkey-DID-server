using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace CAServer.UserAssets.Dtos;

public class GetNftItemInfosDto : PagedResultRequestDto
{
    public List<GetNftItemInfo> GetNftItemInfos { get; set; }
}

public class GetNftItemInfo
{
    public string Symbol { get; set; }
    public string ChainId { get; set; }
}