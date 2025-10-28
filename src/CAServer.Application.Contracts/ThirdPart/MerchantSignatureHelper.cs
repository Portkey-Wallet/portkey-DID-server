using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Cryptography;
using Google.Protobuf;

namespace CAServer.ThirdPart;

public static class MerchantSignatureHelper
{
    public const string SignatureField = "signature";


    public static string GetSignature(string primaryKey, object data)
    {
        var rawData = ThirdPartHelper.ConvertObjectToSortedString(data, SignatureField);
        return GetSignature(primaryKey, rawData);
    }

    public static string GetRawData( object data)
    {
        return ThirdPartHelper.ConvertObjectToSortedString(data, SignatureField);
    }

    public static string GetSignature(string privateKey, string rawData)
    {
        var privateKeyByte = ByteArrayHelper.HexStringToByteArray(privateKey);
        var dataHash = HashHelper.ComputeFrom(rawData);
        var signByte = CryptoHelper.SignWithPrivateKey(privateKeyByte, dataHash.ToByteArray());
        return ByteString.CopyFrom(signByte).ToBase64();
    }

    public static bool VerifySignature(string publicKey, string signature, string rawData)
    {
        var dataHash = HashHelper.ComputeFrom(rawData).ToByteArray();
        var publicKeyByte = ByteArrayHelper.HexStringToByteArray(publicKey);
        var signByte = ByteString.FromBase64(signature);
        return CryptoHelper.VerifySignature(signByte.ToByteArray(), dataHash, publicKeyByte);
    }


}