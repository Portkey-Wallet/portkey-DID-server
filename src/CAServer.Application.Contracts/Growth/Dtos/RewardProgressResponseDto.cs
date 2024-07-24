using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class RewardProgressResponseDto
{

    public List<Dictionary<Tkey, Value>> Data { get; set; }

    public string RewardProcessCount { get; set; }

    // public int SingUpCount { get; set; }
    //
    // public int HamsterCount { get; set; }
    //
    // public double RewardProcessCount { get; set; }

}

public class Tkey
{
    public string Name { get; set; }
}

public class Value
{
    public string Count { get; set; }
}

