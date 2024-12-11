using AElf.Client.Dto;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Dtos;
using CAServer.Hubs;
using Portkey.Contracts.CA;
using ProjectDelegateInfo = CAServer.Dtos.ProjectDelegateInfo;

namespace CAServer.Grains.Grain.ApplicationHandler;

[GenerateSerializer]
public class TransactionInfoDto
{
    [Id(0)]
    public Transaction Transaction { get; set; }
    
    [Id(1)]
    public TransactionResultDto TransactionResultDto { get; set; }
}

[GenerateSerializer]
public class ChainInfo
{
    [Id(0)]
    public string ChainId { get; set; }

    [Id(1)]
    public string BaseUrl { get; set; }

    [Id(2)]
    public string ContractAddress { get; set; }

    [Id(3)]
    public string TokenContractAddress { get; set; }

    [Id(4)]
    public string CrossChainContractAddress { get; set; }

    [Id(5)]
    public string PublicKey { get; set; }

    [Id(6)]
    public bool IsMainChain { get; set; }
}

[GenerateSerializer]
public class CreateHolderDto : ContractDtoBase
{
    [Id(0)]
    public GuardianInfo GuardianInfo { get; set; }
    
    [Id(1)]
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
}

[GenerateSerializer]
public class SocialRecoveryDto : ContractDtoBase
{
    [Id(0)]
    public List<GuardianInfo> GuardianApproved { get; set; }
    
    [Id(1)]
    public Hash LoginGuardianIdentifierHash { get; set; }
}

[GenerateSerializer]
public class ContractDtoBase
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string GrainId { get; set; }

    [Id(2)]
    public string ChainId { get; set; }

    [Id(3)]
    public ManagerInfo ManagerInfo { get; set; }

    [Id(4)]
    public Hash CaHash { get; set; }

    [Id(5)]
    public Address CaAddress { get; set; }

    [Id(6)]
    public HubRequestContext Context { get; set; }

    [Id(7)]
    public ReferralInfo ReferralInfo { get; set; }
    [Id(8)]
    public Platform Platform { get; set; }
}