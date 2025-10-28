using AElf;
using AElf.Cryptography;

namespace CAServer.Commons;

public static class SignatureKeyHelp
{
    public static string CommonPrivateKeyForCallTx { get; set; } = CryptoHelper.GenerateKeyPair().PrivateKey.ToHex();
}