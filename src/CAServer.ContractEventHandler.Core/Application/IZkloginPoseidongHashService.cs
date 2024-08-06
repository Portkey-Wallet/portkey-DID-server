using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.CAAccount;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Guardian;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IZkloginPoseidongHashService
{
    public Task DoWorkAsync(List<string> caHashList);
}

public class ZkloginPoseidongHashService : IZkloginPoseidongHashService, ISingletonDependency
{
    private readonly IPoseidonIdentifierHashProvider _poseidonProvider;
    private readonly ILogger<ZkloginPoseidongHashService> _logger;
    private readonly IGuardianUserProvider _guardianUserProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IContractProvider _contractProvider;

    public ZkloginPoseidongHashService(
        IPoseidonIdentifierHashProvider poseidonProvider,
        ILogger<ZkloginPoseidongHashService> logger,
        IGuardianUserProvider guardianUserProvider,
        IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider)
    {
        _poseidonProvider = poseidonProvider;
        _guardianUserProvider = guardianUserProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _chainOptions = chainOptions.Value;
    }

    public async Task DoWorkAsync(List<string> caHashList)
    {
        _logger.LogInformation("SyncronizeZkloginPoseidonHash event handler starting.........");
        var sw = new Stopwatch();
        sw.Start();
        // var caHoldersByPage = await _contactProvider.GetAllCaHolderAsync(0, 10);
        // var caHashList = caHoldersByPage.Select(holder => holder.CaHash).ToList();
        // List<string> caHashList = new List<string>() { "d2188a2ea94803efe27e4a04e63c26840b4d656ea2e88172f9bdb7dfdaea3f96" };
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
                    var guardianFromEs =
                        guardiansFromEs.FirstOrDefault(g => g.IdentifierHash.Equals(guardian.IdentifierHash.ToHex()));
                    if (!CheckGuardianInfo(guardianFromEs, guardian))
                    {
                        continue;
                    }

                    var poseidonHash = _poseidonProvider.GenerateIdentifierHash(guardianFromEs.Identifier,
                        ByteArrayHelper.HexStringToByteArray(guardianFromEs.Salt));
                    _logger.LogInformation("identifier:{0} (poseidon)identifierHash:{1} salt:{2}",
                        guardianFromEs.Identifier, poseidonHash, (guardianFromEs.Salt));
                    //save poseidon hash in mongodb and es
                    await _guardianUserProvider.AppendGuardianPoseidonHashAsync(guardianFromEs.Identifier,
                        poseidonHash);
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
                contractRequest[getHolderInfoOutput.Key] ??= new RepeatedField<AppendGuardianInput>();
                contractRequest[getHolderInfoOutput.Key].Add(appendGuardianInput);
            }
        }

        var tasks = contractRequest
            .Select(r => ContractInvocationTask(r.Key, r.Value)).ToList();
        await Task.WhenAll(tasks);
        sw.Stop();
        _logger.LogInformation("SyncronizeZkloginPoseidonHashWorker ending... cost:{0}ms", sw.ElapsedMilliseconds);
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
            _logger.LogError("SyncronizeZkloginPoseidonHashWorker invoke contract error resultCreateCaHolder:{0}",
                JsonConvert.SerializeObject(resultCreateCaHolder));
        }

        // else
        // {
        //     await VerifiedPoseidonHashResult(getHolderInfoOutput, caHash);
        // }
        sw.Stop();
        _logger.LogInformation("Invocation contract chainId:{0} cost:{1}ms", chainId, sw.ElapsedMilliseconds);
    }

    private async Task VerifiedPoseidonHashResult(KeyValuePair<string, GetHolderInfoOutput> getHolderInfoOutput,
        string caHash)
    {
        var retryTimes = 0;
        while (retryTimes < 6)
        {
            var holderInfoOutput =
                await _contractProvider.GetHolderInfoFromChainAsync(getHolderInfoOutput.Key, null, caHash);
            var queryResult =
                holderInfoOutput.GuardianList.Guardians.All(g => !g.PoseidonIdentifierHash.IsNullOrEmpty());
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
            _logger.LogError("guardian from contract doesn't exist in es, guardian:{0}",
                JsonConvert.SerializeObject(guardian));
            return false;
        }

        if (!guardian.Salt.Equals(guardianFromEs?.Salt))
        {
            _logger.LogError("guardian from contract has different salt from es, guardian:{0}, guardianFromEs:{1}",
                JsonConvert.SerializeObject(guardian), JsonConvert.SerializeObject(guardianFromEs));
            return false;
        }

        return true;
    }

    private static List<string> ExtractGuardianIdentifierHashFromChains(List<GetHolderInfoOutput> caHolderResult)
    {
        var identifierHash = new List<string>();
        foreach (var getHolderInfoOutput in caHolderResult)
        {
            identifierHash.AddRange(getHolderInfoOutput.GuardianList.Guardians.Select(g => g.IdentifierHash.ToHex())
                .ToList());
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