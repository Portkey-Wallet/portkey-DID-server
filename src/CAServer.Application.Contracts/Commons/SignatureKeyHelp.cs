using System;
using System.Security.Cryptography;

namespace CAServer.Commons;

public static class SignatureKeyHelp
{
    public static string  CommonPrivateKeyForCallTx { get; set; } = Convert.ToBase64String(ECDsa.Create().ExportPkcs8PrivateKey());
}