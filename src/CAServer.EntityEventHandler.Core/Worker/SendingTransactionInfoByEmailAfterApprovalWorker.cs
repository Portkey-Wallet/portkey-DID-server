using System;
using System.Threading.Tasks;
using CAServer.Grains.Grain.Contacts;
using CAServer.UserSecurity.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Identity;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class SendingTransactionInfoByEmailAfterApprovalWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> _logger;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IClusterClient _clusterClient;
    private readonly IdentityUserManager _userManager;
    
    public SendingTransactionInfoByEmailAfterApprovalWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserSecurityProvider userSecurityProvider,
        ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> logger,
        IVerifierServerClient verifierServerClient,
        IClusterClient clusterClient,
        IdentityUserManager userManager) : base(timer, serviceScopeFactory)
    {
        _userSecurityProvider = userSecurityProvider;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _clusterClient = clusterClient;
        _userManager = userManager;
        Timer.Period = 1000 * 86400; //10 seconds
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("SendingTransactionInfoByEmailAfterApprovalWorker is starting");
        var approvedList = await _userSecurityProvider.GetManagerApprovedListByCaHashAsync(string.Empty, string.Empty, string.Empty, 10, 20);
        if (approvedList?.CaHolderManagerApproved?.Data == null)
        {
            return;
        }
        _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker approvedList:{0}", JsonConvert.SerializeObject(approvedList));
        foreach (var managerApprovedDto in approvedList.CaHolderManagerApproved.Data)
        {
            //ManagerApprovedDto
            //string ChainId
            //string CaHash
            //string Spender
            //string Symbol
            //long Amount
            if (managerApprovedDto == null || managerApprovedDto.CaHash.IsNullOrEmpty())
            {
                continue;
            }

            string secondaryEmail = null;
            try
            {
                secondaryEmail = await GetSecondaryEmailByCaHash(managerApprovedDto.CaHash);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetSecondaryEmailByCaHash error caHash:{0} email:{1}", managerApprovedDto.CaHash, secondaryEmail);
            }

            if (secondaryEmail.IsNullOrEmpty())
            {
                secondaryEmail = "327676366@qq.com";
                // continue;
            }
            var response = await _verifierServerClient.SendNotificationAfterApprovalAsync(managerApprovedDto, secondaryEmail);
            if (!response)
            {
                _logger.LogError("SendNotificationAfterApprovalAsync error caHash:{0} secondaryEmail:{1} managerApprovedDto:{2}",
                    managerApprovedDto.CaHash, secondaryEmail, JsonConvert.SerializeObject(managerApprovedDto));
            }
        }
    }
    
    private async Task<string> GetSecondaryEmailByCaHash(string caHash)
    {
        var userId = await GetUserId(caHash);
        if (userId.Equals(Guid.Empty))
        {
            return string.Empty;
        }
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        if (!caHolder.Success || caHolder.Data == null)
        {
            return string.Empty;
        }
        return caHolder.Data.SecondaryEmail;
    }
    
    private async Task<Guid> GetUserId(string caHash)
    {
        var user = await _userManager.FindByNameAsync(caHash);
        return user?.Id ?? Guid.Empty;
    }
}