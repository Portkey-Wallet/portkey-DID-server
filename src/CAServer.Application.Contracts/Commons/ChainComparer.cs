using System;
using System.Collections.Generic;

namespace CAServer.Commons;

public class ChainComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        if (x.Equals(y)) return 0;

        if ((x == CommonConstant.TDVVChainId || x == CommonConstant.TDVWChainId) && y == CommonConstant.MainChainId)
            return -1;
        if (x == CommonConstant.MainChainId && (y == CommonConstant.TDVVChainId || y == CommonConstant.TDVWChainId))
            return 1;
        
        if (x == CommonConstant.TDVVChainId)
            return -1;
        if (y == CommonConstant.TDVVChainId)
            return 1;
        if (x == CommonConstant.TDVWChainId)
            return -1;
        if (y == CommonConstant.TDVWChainId)
            return 1;
        
        if (x == CommonConstant.MainChainId)
            return -1;
        if (y == CommonConstant.MainChainId)
            return 1;
            
        return string.Compare(x, y, StringComparison.Ordinal);
    }
}