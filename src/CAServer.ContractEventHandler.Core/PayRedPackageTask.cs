using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Etos;
using CAServer.Signature;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using IContractProvider = CAServer.Common.IContractProvider;

namespace CAServer.ContractEventHandler.Core;


public class PayRedPackageTask
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<PayRedPackageTask> _logger;
    private readonly IRedPackageAppService _redPackageAppService;
    private readonly IContractProvider _contractProvider;
    private readonly PayRedPackageAccount _packageAccount;
    

    public PayRedPackageTask(IClusterClient clusterClient, ILogger<PayRedPackageTask> logger, 
        IRedPackageAppService redPackageAppService, IOptionsSnapshot<PayRedPackageAccount> packageAccount, IContractProvider contractProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _redPackageAppService = redPackageAppService;
        _packageAccount = packageAccount.Value;
        _contractProvider = contractProvider;
    }

    public async void PayRedPackageAsync(RedPackageCreateEto input)
    {
        _logger.Info("PayRedPackageAsync start and the redpackage id is {}",input.RedPackageId);
        var grabItemDtos = input.Items;
        Debug.Assert(grabItemDtos.IsNullOrEmpty(),"there are no one claim the red packages");
        
        foreach (var item in grabItemDtos)
        {
            //get one our account，pay aelf to user’s account 
            var payRedPackageFrom = _packageAccount.getOneAccountRandom();
        
            //red package transaction
            var result = await _contractProvider.SendTransferAsync(input.Symbol,item.Amount.ToString(),
                item.CaAddress,input.ChainId,payRedPackageFrom);
        }
        _logger.Info("PayRedPackageAsync end and the redpackage id is {}",input.RedPackageId);

        

    }
    
    

}