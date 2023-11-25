using AElf;
using AElf.Client.Service;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;

namespace SignatureServer.Dtos;

public class AccountHolder
{

    private readonly byte[] _privateKey;
    
    public Address Address { get; set; }
    public string PublicKey { get; set; }
    
    private static ECKeyPair GetAElfKeyPair(string privateKeyHex) => CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKeyHex));
    
    public AccountHolder(string privateKey)
    {
        _privateKey = ByteArrayHelper.HexStringToByteArray(privateKey);
        Address = Address.FromPublicKey(GetAElfKeyPair(privateKey).PublicKey);
        var keyPair = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey));
        PublicKey = keyPair.PublicKey.ToHex();
    }

    public Address AddressObj()
    {
        return Address;
    }

    public ByteString GetSignatureWith(byte[] txData)
    {
        var signature = CryptoHelper.SignWithPrivateKey(_privateKey, txData);
        return ByteString.CopyFrom(signature);
    }
}