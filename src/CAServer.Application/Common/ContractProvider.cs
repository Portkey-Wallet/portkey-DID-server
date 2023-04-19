using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAServer.Options;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;

    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
    }


    public async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainOption.PrivateKey);
        const string methodName = "GetVerifierServers";

        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.ContractAddress, methodName, param);

        _logger.LogDebug("param is: {transaction}, privateKey is:{privateKey} ", transaction, chainOption.PrivateKey);

        var txWithSign = client.SignTransaction(chainOption.PrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetVerifierServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }
}