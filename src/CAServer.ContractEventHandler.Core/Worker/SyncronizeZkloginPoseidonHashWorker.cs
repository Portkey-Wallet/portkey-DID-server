using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Guardian;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using ChainOptions = CAServer.ContractEventHandler.Core.Application.ChainOptions;


namespace CAServer.ContractEventHandler.Core.Worker;

public class SyncronizeZkloginPoseidonHashWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPoseidonIdentifierHashProvider _poseidonProvider;
    private readonly ILogger<SyncronizeZkloginPoseidonHashWorker> _logger;
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IContractProvider _contractProvider;
    
    public SyncronizeZkloginPoseidonHashWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IPoseidonIdentifierHashProvider poseidonProvider,
        ILogger<SyncronizeZkloginPoseidonHashWorker> logger,
        IGuardianUserProvider guardianUserProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider) : base(timer, serviceScopeFactory)
    {
        _poseidonProvider = poseidonProvider;
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _chainOptions = chainOptions.Value;
        
        Timer.Period = 10000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker starting.........");
        var sw = new Stopwatch();
        sw.Start();
        // var caHoldersByPage = await _contactProvider.GetAllCaHolderAsync(0, 10);
        // var caHashList = caHoldersByPage.Select(holder => holder.CaHash).ToList();
        List<string> caHashList = new List<string>() { "d2188a2ea94803efe27e4a04e63c26840b4d656ea2e88172f9bdb7dfdaea3f96" };
        foreach (var caHash in caHashList)
        {
            var chainIdToCaHolder = await ListHolderInfosFromContract(caHash);
            if (chainIdToCaHolder.IsNullOrEmpty())
            {
                continue;
            }
            
            var identifierHashList = ExtractGuardianIdentifierHashFromChains(chainIdToCaHolder.Values.ToList());
            var guardiansFromEs = await _guardianUserProvider.GetGuardianListAsync(identifierHashList);
            foreach (var getHolderInfoOutput in chainIdToCaHolder)
            {
                var guardiansOfAppendInput = new RepeatedField<GuardianInfoWithPoseidon>();
                foreach (var guardian in getHolderInfoOutput.Value.GuardianList.Guardians)
                {
                    var guardianFromEs = guardiansFromEs.FirstOrDefault(g => g.IdentifierHash.Equals(guardian.IdentifierHash.ToHex()));
                    if (!CheckGuardianInfo(guardianFromEs, guardian))
                    {
                        continue;
                    }
                    var poseidonHash = _poseidonProvider.GenerateIdentifierHash(guardianFromEs.Identifier, ByteArrayHelper.HexStringToByteArray(guardianFromEs.Salt));
                    _logger.LogInformation("identifier:{0} (poseidon)identifierHash:{1} salt:{2}", guardianFromEs.Identifier, poseidonHash, (guardianFromEs.Salt));
                    //save poseidon hash in mongodb and es
                    await _guardianUserProvider.AppendGuardianPoseidonHashAsync(guardianFromEs.Identifier, poseidonHash);
                    guardiansOfAppendInput.Add(new GuardianInfoWithPoseidon
                    {
                        Type = guardian.Type,
                        IdentifierHash = guardian.IdentifierHash,
                        PoseidonIdentifierHash = poseidonHash
                    });
                }
                var appendGuardianInput = new AppendGuardianInput()
                {
                    CaHash = Hash.LoadFromHex(caHash),
                    Guardians = { guardiansOfAppendInput }
                };
                var resultCreateCaHolder = await _contractProvider.AppendGuardianPoseidonHashAsync(getHolderInfoOutput.Key, appendGuardianInput);
                if (resultCreateCaHolder.Status != TransactionState.Mined)
                {
                    _logger.LogError("SyncronizeZkloginPoseidonHashWorker invoke contract error resultCreateCaHolder:{0}", JsonConvert.SerializeObject(resultCreateCaHolder));
                }
                else
                {
                    await VerifiedPoseidonHashResult(getHolderInfoOutput, caHash);
                }
            }
        }
        sw.Stop();
        _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker ending... cost:{0}ms", sw.ElapsedMilliseconds);
    }

    private async Task VerifiedPoseidonHashResult(KeyValuePair<string, GetHolderInfoOutput> getHolderInfoOutput, string caHash)
    {
        var retryTimes = 0;
        while (retryTimes < 6)
        {
            var holderInfoOutput = await _contractProvider.GetHolderInfoFromChainAsync(getHolderInfoOutput.Key, null, caHash);
            var queryResult = holderInfoOutput.GuardianList.Guardians.All(g => !g.PoseidonIdentifierHash.IsNullOrEmpty());
            _logger.LogInformation("GetHolderInfoFromChain retried {0} times, result:{1}", retryTimes, queryResult);
            if (queryResult)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            retryTimes++;
        }
    }

    private bool CheckGuardianInfo(GuardianIndexDto guardianFromEs, Portkey.Contracts.CA.Guardian guardian)
    {
        
        if (guardianFromEs == null)
        {
            _logger.LogError("guardian from contract doesn't exist in es, guardian:{0}", JsonConvert.SerializeObject(guardian));
            return false;
        }
        if (!guardian.Salt.Equals(guardianFromEs?.Salt))
        {
            _logger.LogError("guardian from contract has different salt from es, guardian:{0}, guardianFromEs:{1}", JsonConvert.SerializeObject(guardian), JsonConvert.SerializeObject(guardianFromEs));
            return false;
        }

        return true;
    }

    private static List<string> ExtractGuardianIdentifierHashFromChains(List<GetHolderInfoOutput> caHolderResult)
    {
        var identifierHash = new List<string>();
        foreach (var getHolderInfoOutput in caHolderResult)
        {
            identifierHash.AddRange(getHolderInfoOutput.GuardianList.Guardians.Select(g => g.IdentifierHash.ToHex()).ToList());
        }

        return identifierHash.Distinct().ToList();
    }

    private async Task<Dictionary<string, GetHolderInfoOutput>> ListHolderInfosFromContract(string caHash)
    {
        var sw = new Stopwatch();
        sw.Start();
        var chainIds = _chainOptions.ChainInfos.Keys;
        var result = new Dictionary<string, GetHolderInfoOutput>();
        foreach (var chainId in chainIds)
        {
            var getHolderInfoOutput = await _contractProvider.GetHolderInfoFromChainAsync(chainId, null, caHash);
            result[chainId] = getHolderInfoOutput;
        }
        sw.Stop();
        _logger.LogInformation("GetHolderInfoFromChainAsync cost:{0}ms", sw.ElapsedMilliseconds);
        return result;
    }
}