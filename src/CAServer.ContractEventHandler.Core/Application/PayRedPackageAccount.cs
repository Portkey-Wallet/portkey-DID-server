using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CAServer.ContractEventHandler.Core.Application;

public class PayRedPackageAccount
{
    public List<string> RedPackagePayAccounts { get; set; }

    private static readonly object _lockObject = new object();
    public string getOneAccountRandom()
    {
        lock (_lockObject)
        {
            Console.WriteLine($"RedPackagePayAccounts {JsonConvert.SerializeObject(RedPackagePayAccounts)}");
            Debug.Assert(RedPackagePayAccounts.IsNullOrEmpty(),
                "we can not find pay red package from account");
            Random rd = new Random();
            int path = rd.Next();
            return RedPackagePayAccounts[path % RedPackagePayAccounts.Count];
        }
    }
}
