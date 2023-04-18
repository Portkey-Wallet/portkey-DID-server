using AElf.Types;

namespace CAServer.Grains.State.ApplicationHandler;

public class HolderState
{
    public Hash CaHash { get; set; }
    public List<string> LoginGuardianTypesMainChain { get; set; }
    public List<string> LoginGuardianTypesSideChain { get; set; }
}