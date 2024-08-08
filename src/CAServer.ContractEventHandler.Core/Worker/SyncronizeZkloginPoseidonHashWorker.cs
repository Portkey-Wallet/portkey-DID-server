using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos.Zklogin;
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
        //todo put 86400 in apollo config
        if (!await _registrarProvider.RegisterUniqueWorkerNodeAsync(WorkerName, 86400, 86400))
        {
            return;
        }
        _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker starting.........");
        var sw = new Stopwatch();
        sw.Start();
        var total = 14600; //todo put the param in apollo config
        var times = total / 30 + 1; //todo put 30 size in apollo config
        var saveErrorPoseidonDtos = new List<ZkPoseidonDto>();
        for (var i = 0; i < times; i++)
        {
            var swLoop = new Stopwatch();
            swLoop.Start();
            try
            {
                var singleLoopResult = await SaveDataUnderChainAndHandlerOnChainData(i * 30, 30);
                if (singleLoopResult.Item1 && !singleLoopResult.Item2.IsNullOrEmpty())
                {
                    saveErrorPoseidonDtos.AddRange(singleLoopResult.Item2);   
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SaveDataUnderChainAndHandlerOnChainData error skip:{0}, limit:30", i * 100);
            }
            swLoop.Stop();
            _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker loop:{0} cost:{1}ms", i, swLoop.ElapsedMilliseconds);
        }

        if (!saveErrorPoseidonDtos.IsNullOrEmpty())
        {
            _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker save error guardians:{0}", JsonConvert.SerializeObject(saveErrorPoseidonDtos));
        }
        sw.Stop();
        _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker ending... cost:{0}ms", sw.ElapsedMilliseconds);
    }

    private async Task<(bool, List<ZkPoseidonDto>)> SaveDataUnderChainAndHandlerOnChainData(int skip, int limit)
    {
        var saveErrorPoseidonDtos = new List<ZkPoseidonDto>();
        var guardiansDto = await _contactProvider.GetCaHolderInfoAsync(new List<string>() { }, string.Empty, skip, limit);
        if (guardiansDto == null || guardiansDto.CaHolderInfo.IsNullOrEmpty())
        {
            _logger.LogInformation("SaveDataUnderChainAndHandlerOnChainData finished last loop skip:{0} limit:{1}", skip, limit);
            return new ValueTuple<bool, List<ZkPoseidonDto>>(false, saveErrorPoseidonDtos);
        }
        var caHashList = guardiansDto.CaHolderInfo.Select(ca => ca.CaHash).ToList();
        // data from es, less than 8000
        // var caHoldersByPage = await _contactProvider.GetAllCaHolderAsync(skip, limit);
        // var caHashList = caHoldersByPage.Select(holder => holder.CaHash).ToList();
        var contractRequest = new Dictionary<string, RepeatedField<AppendGuardianInput>>();
        //users' loop
        foreach (var caHash in caHashList)
        {
            var chainIdToCaHolder = await ListHolderInfosFromContract(caHash);
            if (chainIdToCaHolder.IsNullOrEmpty())
            {
                continue;
            }

            var identifierHashList = ExtractGuardianIdentifierHashFromChains(chainIdToCaHolder);
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
                    var mongoEsAppendResult = await _guardianUserProvider.AppendGuardianPoseidonHashAsync(guardianFromEs.Identifier, poseidonHash);
                    if (!mongoEsAppendResult)
                    {
                        saveErrorPoseidonDtos.Add(new ZkPoseidonDto()
                        {
                            ChainId = getHolderInfoOutput.Key,
                            CaHash = caHash,
                            GuardianIdentifier = guardianFromEs.Identifier,
                            Salt = guardianFromEs.Salt,
                            PoseidonHash = poseidonHash,
                            ErrorMessage = "append to mongo and es error"
                        });
                        continue;
                    }
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
            .Select(r => ContractInvocationTask(r.Key, r.Value, saveErrorPoseidonDtos)).ToList();
        await Task.WhenAll(tasks);
        return new ValueTuple<bool, List<ZkPoseidonDto>>(true, saveErrorPoseidonDtos);
    }

    private async Task ContractInvocationTask(string chainId, RepeatedField<AppendGuardianInput> inputs, List<ZkPoseidonDto> saveErrorPoseidonDtos)
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
            foreach (var appendGuardianInput in inputs)
            {
                saveErrorPoseidonDtos.AddRange(appendGuardianInput.Guardians.Select(poseidonGuardian => new ZkPoseidonDto()
                {
                    ChainId = chainId,
                    CaHash = appendGuardianInput.CaHash.ToHex(),
                    IdentifierHash = poseidonGuardian.IdentifierHash.ToHex(),
                    GuardianType = poseidonGuardian.Type,
                    PoseidonHash = poseidonGuardian.PoseidonHash,
                    ErrorMessage = "append to contract error"
                }));
            }
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

    private static List<string> ExtractGuardianIdentifierHashFromChains(Dictionary<string, GetHolderInfoOutput> chainIdToCaHolder)
    {
        foreach (var chainId2HolderInfoOutput in chainIdToCaHolder)
        {
            var createChainId = ChainHelper.ConvertChainIdToBase58(chainId2HolderInfoOutput.Value.CreateChainId);
            if (chainId2HolderInfoOutput.Key.Equals(createChainId))
            {
                return chainId2HolderInfoOutput.Value.GuardianList.Guardians
                    .Select(g => g.IdentifierHash.ToHex())
                    .ToList();
            }
        }

        return new List<string>();
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