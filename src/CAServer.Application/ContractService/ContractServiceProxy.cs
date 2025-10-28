using System.Threading.Tasks;
using AElf.Client.Dto;
using CAServer.CAAccount;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using CAServer.Options;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractService;

public class ContractServiceProxy : ISingletonDependency
{
    private readonly ILogger<ContractServiceProxy> _logger;
    private readonly IContractService _contractService;
    private readonly IContractAppProvider _contractAppProvider;
    private readonly IOptionsMonitor<ContractServiceOptions> _contractServiceOptions;

    public ContractServiceProxy(ILogger<ContractServiceProxy> logger, IContractService contractService,
        IContractAppProvider contractAppProvider, IOptionsMonitor<ContractServiceOptions> contractServiceOptions)
    {
        _logger = logger;
        _contractService = contractService;
        _contractAppProvider = contractAppProvider;
        _contractServiceOptions = contractServiceOptions;
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.CreateHolderInfoAsync(createHolderDto);
            default:
                return await _contractService.CreateHolderInfoAsync(createHolderDto);
        }
    }

    public async Task<TransactionResultDto> CreateHolderInfoOnNonCreateChainAsync(string chainId,
        CreateHolderDto createHolderDto)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.CreateHolderInfoOnNonCreateChainAsync(chainId, createHolderDto);
            default:
                return await _contractService.CreateHolderInfoOnNonCreateChainAsync(chainId, createHolderDto);
        }
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.SocialRecoveryAsync(socialRecoveryDto);
            default:
                return await _contractService.SocialRecoveryAsync(socialRecoveryDto);
        }
    }

    public async Task<TransactionInfoDto> ValidateTransactionAsync(string chainId, GetHolderInfoOutput output,
        RepeatedField<string> unsetLoginGuardianTypes)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.ValidateTransactionAsync(chainId, output, unsetLoginGuardianTypes);
            default:
                return await _contractService.ValidateTransactionAsync(chainId, output, unsetLoginGuardianTypes);
        }
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfo transactionInfo)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.GetSyncHolderInfoInputAsync(chainId, transactionInfo);
            default:
                return await _contractService.GetSyncHolderInfoInputAsync(chainId, transactionInfo);
        }
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.SyncTransactionAsync(chainId, input);
            default:
                return await _contractService.SyncTransactionAsync(chainId, input);
        }
    }

    public async Task<TransactionResultDto> ForwardTransactionAsync(string chainId, string rawTransaction)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.ForwardTransactionAsync(chainId, rawTransaction);
            default:
                return await _contractService.ForwardTransactionAsync(chainId, rawTransaction);
        }
    }

    public async Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(string chainId, IMessage param,
        string payRedPackageFrom,
        string redPackageContractAddress, string methodName)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.SendTransferRedPacketToChainAsync(chainId, param, payRedPackageFrom,
                    redPackageContractAddress, methodName);
            default:
                return await _contractService.SendTransferRedPacketToChainAsync(chainId, param, payRedPackageFrom,
                    redPackageContractAddress, methodName);
        }
    }

    public async Task<TransactionResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeDto assignProjectDelegateeDto)
    {
        switch (_contractServiceOptions.CurrentValue.UseGrainService)
        {
            case true:
                return await _contractAppProvider.AuthorizeDelegateAsync(assignProjectDelegateeDto);
            default:
                return await _contractService.AuthorizeDelegateAsync(assignProjectDelegateeDto);
        }
    }
}