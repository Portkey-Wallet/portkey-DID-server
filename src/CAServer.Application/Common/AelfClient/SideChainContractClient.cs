using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Cryptography;
using AElf.Types;
using CAServer.Commons;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.Common.AelfClient;

public class SideChainContractClient : IContractClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SideChainContractClient> _logger;

    public SideChainContractClient(IHttpClientFactory httpClientFactory, ILogger<SideChainContractClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Transaction> GenerateTransactionAsync(
        string from,
        string to,
        string methodName,
        IMessage input)
    {
        try
        {
            var chainStatusAsync = await GetChainStatusAsync();
            return new Transaction()
            {
                From = Address.FromBase58(from),
                To = Address.FromBase58(to),
                MethodName = methodName,
                Params = input.ToByteString(),
                RefBlockNumber = chainStatusAsync.BestChainHeight,
                RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(chainStatusAsync.BestChainHash).Value
                    .Take<byte>(4).ToArray<byte>())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContractClient.GenerateTransactionAsync] SideChainContractClient error, msg:{0},trace:{1}", ex.Message,
                ex.StackTrace ?? "-");
            return (Transaction)null;
        }
    }

    public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
    {
        var uri = string.Format("api/blockChain/blockByHeight?blockHeight={0}&includeTransactions={1}",
            blockHeight, includeTransactions);
        return await GetAsync<BlockDto>(uri);
    }

    public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
    {
        var uri = "api/blockChain/sendTransaction";
        var param = new Dictionary<string, string>()
        {
            {
                "RawTransaction",
                input.RawTransaction
            }
        };
        return await PostAsync<SendTransactionOutput>(uri, param);
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
    {
        var url = "api/blockChain/transactionResult?transactionId=" + transactionId;
        return await GetAsync<TransactionResultDto>(url);
    }

    public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
    {
        var url = "api/blockChain/executeTransaction";
        var param = new Dictionary<string, string>()
        {
            {
                "RawTransaction",
                input.RawTransaction
            }
        };
        return await PostAsync(url, param);
    }
    
    public Transaction SignTransaction(string privateKeyHex, Transaction transaction)
    {
        byte[] byteArray = transaction.GetHash().ToByteArray();
        byte[] numArray =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKeyHex), byteArray);
        transaction.Signature = ByteString.CopyFrom(numArray);
        return transaction;
    }

    public async Task<ChainStatusDto> GetChainStatusAsync()
    {
        var uri = "api/blockChain/chainStatus";
        return await GetAsync<ChainStatusDto>(uri);
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var client = _httpClientFactory.CreateClient(AelfClientConstant.SideChainClient);
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] SideChainContractClient GetError Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }
        
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(AelfClientConstant.SideChainClient);

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] SideChainContractClient PostError Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }
        
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<string> PostAsync(string url, object paramObj)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(AelfClientConstant.SideChainClient);

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] SideChainContractClient PostError Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        return content;
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
}