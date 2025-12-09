using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CAServer.ContractEventHandler.Core.Application;

public class PayRedPackageAccount
{
    public List<string> RedPackagePayAccounts { get; set; }

    public string getOneAccountRandom()
    {
        // Debug.Assert(RedPackagePayAccounts.IsNullOrEmpty(),
        //     "we can not find pay red package from account");
        Random rd = new Random();
        int path = rd.Next();
        return RedPackagePayAccounts[path % RedPackagePayAccounts.Count];
    }
}
