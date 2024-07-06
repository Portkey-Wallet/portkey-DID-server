using AElf.Sdk.CSharp.State;
using AetherLink.Contracts.Oracle;

namespace Portkey.Contracts.CA;

public partial class CAContractState : ContractState
{
    internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
}