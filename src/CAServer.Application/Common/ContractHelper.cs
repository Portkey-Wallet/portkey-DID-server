using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAServer.Grains.Grain.ApplicationHandler;
using Google.Protobuf;

namespace CAServer.Common;

public static class ContractHelper
{
    public static async Task<T> CallTransactionAsync<T>(string methodName, IMessage param,
        bool isTokenContract, ChainInfo chainInfo) where T : class, IMessage<T>, new()
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
        var contractAddress = isTokenContract ? chainInfo.TokenContractAddress : chainInfo.ContractAddress;

        var transaction =
            await client.GenerateTransactionAsync(ownAddress, contractAddress,
                methodName, param);
        var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }
}