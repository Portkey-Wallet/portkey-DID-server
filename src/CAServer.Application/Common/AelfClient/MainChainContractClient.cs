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

public class MainChainContractClient : IContractClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MainChainContractClient> _logger;

    public MainChainContractClient(IHttpClientFactory httpClientFactory, ILogger<MainChainContractClient> logger)
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
            _logger.LogError(ex, "[ContractClient.GenerateTransactionAsync] error, msg:{0},trace:{1}", ex.Message,
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
        var result = await PostAsync<SendTransactionOutput>(uri, param);
        return result;
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
    {
        var url = "api/blockChain/transactionResult?transactionId=" + transactionId;
        var res = await GetAsync<TransactionResultDto>(url);
        return res;
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
        var res = await PostAsync(url, param);
        return res;
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
        _logger.LogInformation("[MainChainContractClient] GetAsync begin:{url}", url);
        var client = _httpClientFactory.CreateClient(AelfClientConstant.MainChainClient);
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] GetError Response not success, url:{url}, code:{code}, message: {message}",
                url, response.StatusCode, content);

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        _logger.LogInformation("[MainChainContractClient] GetAsync end:{url}", url);
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<T> PostAsync<T>(string url, object paramObj)
    {
        _logger.LogInformation("[MainChainContractClient] PostAsync<T> begin:{url}", url);
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(AelfClientConstant.MainChainClient);

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] PostError Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        _logger.LogInformation("[MainChainContractClient] PostAsync<T> end:{url}", url);
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task<string> PostAsync(string url, object paramObj)
    {
        _logger.LogInformation("[MainChainContractClient] PostAsync<T> begin:{url}", url);
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(AelfClientConstant.MainChainClient);

        var response = await client.PostAsync(url, requestContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!ResponseSuccess(response.StatusCode))
        {
            _logger.LogError(
                "[ContractClientError] PostError Response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }

        _logger.LogInformation("[MainChainContractClient] PostAsync<T> end:{url}", url);
        return content;
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
}