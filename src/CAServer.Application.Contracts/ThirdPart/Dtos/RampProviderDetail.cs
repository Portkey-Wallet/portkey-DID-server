using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class RampProviderDetail
{
    
    public List<ProviderRampDetail> ProvidersList { get; set; }
    
}


public class ProviderRampDetail
{
    public string ThirdPartName { get; set; }
    public string Price { get; set; }
}