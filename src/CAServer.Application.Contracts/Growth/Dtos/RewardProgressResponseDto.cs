using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class RewardProgressResponseDto
{

    public List<Dictionary<string, string>> Data { get; set; }

    public string RewardProcessCount { get; set; }

}


