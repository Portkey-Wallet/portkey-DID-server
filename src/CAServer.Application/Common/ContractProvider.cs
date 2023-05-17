using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using CAServer.ClaimToken.Dtos;
using CAServer.Options;
using CAServer.Signature;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portkey.Contracts.CA;
using Portkey.Contracts.TokenClaim;
using Volo.Abp.DependencyInjection;
using GetBalanceInput = AElf.Contracts.MultiToken.GetBalanceInput;
using SignatureOptions = CAServer.Options.SignatureOptions;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;
using ChainInfo = CAServer.Grains.Grain.ApplicationHandler.ChainInfo;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Common;

public interface IContractProvider
{
    public Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId);
    public Task<GetBalanceOutput> GetBalanceAsync(string chainId, string address, string symbol);
    public Task<SendTransactionOutput> TransferAsync(string chainId, ClaimTokenRequestDto claimTokenRequestDto);
    public Task ClaimTokenAsync(string chainId, string symbol);

    Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        bool isTokenContract, ChainInfo chainInfo) where T : class, IMessage<T>, new();
}

public class ContractProvider : IContractProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly ILogger<ContractProvider> _logger;
    private readonly ClaimTokenInfoOptions _claimTokenInfoOption;
    private readonly IOptions<SignatureOptions> _signatureOptions;
    private readonly ISignatureProvider _signatureProvider;

    public ContractProvider(IOptions<ChainOptions> chainOptions, ILogger<ContractProvider> logger,
        IOptions<SignatureOptions> signatureOptions,
        ISignatureProvider signatureProvider,
        IOptionsSnapshot<ClaimTokenInfoOptions> claimTokenInfoOption)
    {
        _chainOptions = chainOptions.Value;
        _logger = logger;
        _claimTokenInfoOption = claimTokenInfoOption.Value;
        _signatureOptions = signatureOptions;
        _signatureProvider = signatureProvider;
    }


    public async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(chainOption.PublicKey);
        const string methodName = "GetVerifierServers";

        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.ContractAddress, methodName, param);

        _logger.LogDebug("param is: {transaction}, privateKey is:{privateKey} ", transaction, chainOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTransaction(_signatureOptions.Value.BaseUrl, ownAddress,
            transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });
        return GetVerifierServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }

    public async Task<GetBalanceOutput> GetBalanceAsync(string chainId, string address, string symbol)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(chainOption.PublicKey);
        const string methodName = "GetBalance";
        var param = new GetBalanceInput
        {
            Symbol = symbol,
            Owner = Address.FromBase58(address)
        };
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.TokenContractAddress, methodName, param);

        _logger.LogDebug("param is: {transaction}, privateKey is:{privateKey} ", transaction, chainOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTransaction(_signatureOptions.Value.BaseUrl, ownAddress,
            transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });
        return GetBalanceOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }


    public async Task<SendTransactionOutput> TransferAsync(string chainId, ClaimTokenRequestDto claimTokenRequestDto)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return new SendTransactionOutput();
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(_claimTokenInfoOption.PublicKey);
        const string methodName = "Transfer";
        var param = new TransferInput
        {
            Symbol = claimTokenRequestDto.Symbol,
            Amount = long.Parse(claimTokenRequestDto.Amount),
            To = Address.FromBase58(claimTokenRequestDto.Address)
        };
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.TokenContractAddress, methodName, param);

        _logger.LogDebug("param is: {transaction}, privateKey is:{privateKey} ", transaction,
            _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTransaction(_signatureOptions.Value.BaseUrl, ownAddress,
            transaction.ToByteArray().ToHex());

        var result = await client.SendTransactionAsync(new SendTransactionInput()
        {
            RawTransaction = txWithSign
        });
        return result;
    }

    public async Task ClaimTokenAsync(string chainId, string symbol)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(_claimTokenInfoOption.PublicKey);
        const string methodName = "ClaimToken";
        var param = new ClaimTokenInput
        {
            Symbol = symbol,
            Amount = _claimTokenInfoOption.ClaimTokenAmount
        };
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            _claimTokenInfoOption.ClaimTokenAddress, methodName, param);

        _logger.LogDebug("param is: {transaction}, privateKey is:{privateKey} ", transaction,
            _claimTokenInfoOption.PublicKey);

        var txWithSign = await _signatureProvider.SignTransaction(_signatureOptions.Value.BaseUrl, ownAddress,
            transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });
    }

    public async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, bool isTokenContract,
        ChainInfo chainInfo)
        where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
        var contractAddress = isTokenContract ? chainInfo.TokenContractAddress : chainInfo.ContractAddress;

        var transaction =
            await client.GenerateTransactionAsync(ownAddress, contractAddress,
                methodName, param);

        var txWithSign = await _signatureProvider.SignTransaction(_signatureOptions.Value.BaseUrl, ownAddress,
            transaction.ToByteArray().ToHex());

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }
}