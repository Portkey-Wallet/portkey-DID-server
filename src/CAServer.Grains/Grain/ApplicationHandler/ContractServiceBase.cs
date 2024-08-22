using AElf.Client.Dto;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Dtos;
using CAServer.Hubs;
using Portkey.Contracts.CA;
using ProjectDelegateInfo = CAServer.Dtos.ProjectDelegateInfo;

namespace CAServer.Grains.Grain.ApplicationHandler;

public class TransactionInfoDto
{
    public Transaction Transaction { get; set; }
    public TransactionResultDto TransactionResultDto { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string TokenContractAddress { get; set; }
    public string CrossChainContractAddress { get; set; }
    public string PublicKey { get; set; }
    public bool IsMainChain { get; set; }
}

public class CreateHolderDto : ContractDtoBase
{
    public GuardianInfo GuardianInfo { get; set; }
    
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
}

public class SocialRecoveryDto : ContractDtoBase
{
    public List<GuardianInfo> GuardianApproved { get; set; }
    public Hash LoginGuardianIdentifierHash { get; set; }
}

public class ContractDtoBase
{
    public Guid Id { get; set; }
    public string GrainId { get; set; }
    public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
    public Hash CaHash { get; set; }
    public Address CaAddress { get; set; }
    public HubRequestContext Context { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
}