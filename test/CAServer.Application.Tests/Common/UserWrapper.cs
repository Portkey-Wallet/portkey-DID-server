using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Cryptography;
using Google.Protobuf;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace CAServer.Common;

public class UserWrapper
{
    
    private string PrivateKey { get; set; }
    public string Address { get; set; }
    public string PublicKey { get; set; }
    public AElfClient Client { get; set; }
    
    public UserWrapper(AElfClient client, string privateKey)
    {
        PrivateKey = privateKey;
        Client = client;
        Address = client.GetAddressFromPrivateKey(privateKey);
        var keyPair = CryptoHelper.FromPrivateKey(StringToByteArray(privateKey));
        PublicKey = ByteArrayToString(keyPair.PublicKey);
    }

    public Dictionary<string, object> AddressObj()
    {
        return AelfAddressHelper.ToAddressObj(Address);
    }

    public ByteString GetSignatureWith(byte[] txData)
    {
        var signature = CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(PrivateKey), txData);
        return ByteString.CopyFrom(signature);
    }
    
    public static byte[] StringToByteArray(string hex) 
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }
    
    public static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }
    
    public Task<CreateRawTransactionOutput> CreateRawTransactionAsync(string contractAddress, string contractMethod, object parameters)
    {
        var address = contractAddress;
        var status = Client.GetChainStatusAsync().GetAwaiter().GetResult();
        var height = status.BestChainHeight;
        var blockHash = status.BestChainHash;

        // create row transaction
        var input = new CreateRawTransactionInput
        {
            From = Address,
            To = address,
            MethodName = contractMethod,
            Params = JsonConvert.SerializeObject(parameters, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            }),
            RefBlockNumber = height,
            RefBlockHash = blockHash
        };
        return Client.CreateRawTransactionAsync(input);
    }
}