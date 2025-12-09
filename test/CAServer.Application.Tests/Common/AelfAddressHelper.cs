using System;
using System.Collections.Generic;
using AElf;

namespace CAServer.Common;

public static class AelfAddressHelper
{

    public static string Base58ToBase64(string base58Address)
    {
        return Convert.ToBase64String(Base58CheckEncoding.Decode(base58Address));
    }

    public static Dictionary<string, object> ToAddressObj(string base58Address)
    {
        return new Dictionary<string, object>
        {
            ["value"] = Convert.ToBase64String(Base58CheckEncoding.Decode(base58Address))
        };
    }

}