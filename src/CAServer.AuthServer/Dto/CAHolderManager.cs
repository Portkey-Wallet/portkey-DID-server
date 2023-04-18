using System.Collections.Generic;

namespace CAServer.Dto;

public class CAHolderManager
{
    public string ChainId { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public List<Managers> Managers { get; set; }
}