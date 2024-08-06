using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.Common;
using CAServer.Contacts.Provider;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Guardian;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using ChainOptions = CAServer.ContractEventHandler.Core.Application.ChainOptions;
using IContractProvider = CAServer.ContractEventHandler.Core.Application.IContractProvider;


namespace CAServer.ContractEventHandler.Core.Worker;

public class SyncronizeZkloginPoseidonHashWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPoseidonIdentifierHashProvider _poseidonProvider;
    private readonly ILogger<SyncronizeZkloginPoseidonHashWorker> _logger;
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IContractProvider _contractProvider;
    private readonly IBackgroundWorkerRegistrarProvider _registrarProvider;
    private readonly IContactProvider _contactProvider;
    private const string WorkerName = "SyncronizeZkloginPoseidonHashWorker";
    
    public SyncronizeZkloginPoseidonHashWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IPoseidonIdentifierHashProvider poseidonProvider,
        ILogger<SyncronizeZkloginPoseidonHashWorker> logger,
        IGuardianUserProvider guardianUserProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider,
        IBackgroundWorkerRegistrarProvider registrarProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        IContactProvider contactProvider) : base(timer, serviceScopeFactory)
    {
        _poseidonProvider = poseidonProvider;
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _chainOptions = chainOptions.Value;
        _registrarProvider = registrarProvider;
        _contactProvider = contactProvider;
        
        Timer.Period = 1000 * 86400;
        Timer.RunOnStart = true;
        hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _registrarProvider.TryRemoveWorkerNodeAsync(WorkerName);
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        // if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, 86400, 86400))
        // {
        //     return;
        // }
        // _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker starting.........");
        // var sw = new Stopwatch();
        // sw.Start();
        // var totalHolders = await _contactProvider.GetAllCaHolderWithTotalAsync(0, 1);
        // var total = totalHolders.Item1;
        // var times = total / 100 + 1;
        // for (var i = 0; i < times; i++)
        // {
        //     var swLoop = new Stopwatch();
        //     swLoop.Start();
        //     try
        //     {
        //         await SaveDataUnderChainAndHandlerOnChainData(i * 100, 100);
        //     }
        //     catch (Exception e)
        //     {
        //         _logger.LogError(e, "SaveDataUnderChainAndHandlerOnChainData error skip:{0}, limit:100", i * 100);
        //     }
        //     swLoop.Stop();
        //     _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker loop:{0} cost:{1}ms", i, swLoop.ElapsedMilliseconds);
        // }
        // sw.Stop();
        // _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker ending... cost:{0}ms", sw.ElapsedMilliseconds);
    }

    private async Task SaveDataUnderChainAndHandlerOnChainData(int skip, int limit)
    {
        var caHoldersByPage = await _contactProvider.GetAllCaHolderAsync(skip, limit);
        var caHashList = caHoldersByPage.Select(holder => holder.CaHash).ToList();
        var contractRequest = new Dictionary<string, RepeatedField<AppendGuardianInput>>();
        //users' loop
        foreach (var caHash in caHashList)
        {
            var chainIdToCaHolder = await ListHolderInfosFromContract(caHash);
            if (chainIdToCaHolder.IsNullOrEmpty())
            {
                continue;
            }
            
            var identifierHashList = ExtractGuardianIdentifierHashFromChains(chainIdToCaHolder.Values.ToList());
            var guardiansFromEs = await _guardianUserProvider.GetGuardianListAsync(identifierHashList);
            //chains' loop
            foreach (var getHolderInfoOutput in chainIdToCaHolder)
            {
                var guardiansOfAppendInput = new RepeatedField<PoseidonGuardian>();
                //guardians' loop
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
                    guardiansOfAppendInput.Add(new PoseidonGuardian
                    {
                        Type = guardian.Type,
                        IdentifierHash = guardian.IdentifierHash,
                        PoseidonHash = poseidonHash
                    });
                }
                var appendGuardianInput = new AppendGuardianInput()
                {
                    CaHash = Hash.LoadFromHex(caHash),
                    Guardians = { guardiansOfAppendInput }
                };
                if (!contractRequest.TryGetValue(getHolderInfoOutput.Key, out RepeatedField<AppendGuardianInput> value))
                {
                    value = new RepeatedField<AppendGuardianInput>();
                    contractRequest[getHolderInfoOutput.Key] = value;
                }

                value.Add(appendGuardianInput);
            }
        }

        var tasks = contractRequest
            .Select(r => ContractInvocationTask(r.Key, r.Value)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task ContractInvocationTask(string chainId, RepeatedField<AppendGuardianInput> inputs)
    {
        var sw = new Stopwatch();
        sw.Start();
        var request = new AppendGuardianRequest
        {
            Input = { inputs }
        };
        var resultCreateCaHolder = await _contractProvider.AppendGuardianPoseidonHashAsync(chainId, request);
        if (resultCreateCaHolder.Status != TransactionState.Mined)
        {
            _logger.LogError("SyncronizeZkloginPoseidonHashWorker invoke contract error resultCreateCaHolder:{0}", JsonConvert.SerializeObject(resultCreateCaHolder));
        }
        // else
        // {
        //     await VerifiedPoseidonHashResult(getHolderInfoOutput, caHash);
        // }
        sw.Stop();
        _logger.LogInformation("Invocation contract chainId:{0} cost:{1}ms", chainId, sw.ElapsedMilliseconds);
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
    // List<string> caHashList = new List<string>() { 
    //     "cffe2371fcca50e10515095efb9f03ab7171897252cf523044a2bc952a4f2f29",
    //     "37546fb10af04e681ed65a8bb16d03fe35fdb516b79b9b026a288f945dd12d97",
    //     "271fd1f512bbb4a6e89f1a0f990be00aed6cd348355eed0fe6de62529817ab41",
    //     "4ae341df4cdc13b6d28ae5def6abca431464abb84160fc380772e00568933bbf",
    //     "92872be6b1969f4dd1f0c6c280324ae709a7cb0594d5eebb43f2449ca588603f",
    //     "013dffaa460b4ce053060b2497a0ade59d23093d4087050e9e4747a2ba160383",
    //     "aab857749aa9114f1bbf306d52cc4f9219ab201c6b5ebbab0283e8252e4cac2b",
    //     "15e08db5cb7688f8f52ae5ef5fc9b79c866e5fedc7ca694f5d28f86902875a97",
    //     "f9221e8a600a047f04009a602672c88cfc09f063b2474cc44d901a078299db63",
    //     "d106d48b2f9283c5577c848efb440b4498024335dd01e56236b7b5343af727f1" };
}