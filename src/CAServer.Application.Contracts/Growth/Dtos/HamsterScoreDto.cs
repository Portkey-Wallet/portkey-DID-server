using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ScoreInfos
{
    public List<HamsterScoreDto> GetScoreInfos { get; set; } = new List<HamsterScoreDto>();

}

public class HamsterScoreDto
{
    public string CaAddress { get; set; }
    
    public long SumScore { get; set; }

    public string Symbol { get; set; }
    
    public int Decimals { get; set; }

}