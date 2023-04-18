using AElf.Client.Dto;
using AElf.Types;
using CAServer.Hubs;

namespace CAServer.Grains.Grain.ApplicationHandler;

public class TransactionDto
{
    public Transaction Transaction { get; set; }
    public TransactionResultDto TransactionResultDto { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string PrivateKey { get; set; }
    public bool IsMainChain { get; set; }
}

public class CreateHolderDto : ContractDtoBase
{
    public GuardianAccountInfo GuardianAccountInfo { get; set; }
}

public class SocialRecoveryDto : ContractDtoBase
{
    public List<GuardianAccountInfo> GuardianApproved { get; set; }
    public string LoginGuardianAccount { get; set; }
}

public class ContractDtoBase
{
    public Guid Id { get; set; }
    public string GrainId { get; set; }
    public string ChainId { get; set; }
    public Manager Manager { get; set; }
    public Hash CaHash { get; set; }
    public string CaAddress { get; set; }
    public HubRequestContext Context { get; set; }
}