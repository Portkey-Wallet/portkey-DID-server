using System;
using System.Collections.Generic;
using System.Reflection;
using CAServer.Commons.Etos;

namespace CAServer.Commons;

public static class ChainDisplayNameHelper
{
    public static readonly string MainChain = "aelf MainChain";
    public static readonly string DAppChain = "aelf dAppChain";
    public static readonly string MainChainImageUrl = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/mainChain.png";
    public static readonly string DAppChainImageUrl = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/aelf/dappChain.png";

    public static readonly Dictionary<string, string> DisplayNameMap = new Dictionary<string, string>
    {
        { MainChain, MainChain },
        { DAppChain, DAppChain },
        { CommonConstant.MainChainId, MainChain },
        { CommonConstant.TDVVChainId, DAppChain },
        { CommonConstant.TDVWChainId, DAppChain },
    };
    public static readonly Dictionary<string, string> ChainImageUrlMap = new Dictionary<string, string>
    {
        { MainChain, MainChainImageUrl },
        { DAppChain, DAppChainImageUrl },
        { CommonConstant.MainChainId, MainChainImageUrl },
        { CommonConstant.TDVVChainId, DAppChainImageUrl },
        { CommonConstant.TDVWChainId, DAppChainImageUrl },
    };

    // if not exits, maybe throw exception
    public static string MustGetChainDisplayName(string chainId)
    {
        return DisplayNameMap[chainId];
    }
    
    // if not exits, maybe throw exception
    public static string MustGetChainUrl(string chainId)
    {
        return ChainImageUrlMap[chainId];
    }

    // if not exits, return default
    public static string GetChainDisplayNameByDefault(string chainId)
    {
        return DisplayNameMap.GetValueOrDefault(chainId, DAppChain);
    }

    public static void SetDisplayName<T>(T obj)
    {
        string chainId = GetPropertyValue(obj, "ChainId");
        if (null == chainId)
        {
            return;
        }

        SetPropertyValue(obj, "DisplayChainName", MustGetChainDisplayName(chainId));
        SetPropertyValue(obj, "ChainImageUrl", MustGetChainUrl(chainId));
    }

    public static void SetDisplayName(ChainDisplayNameDto obj, string chainId)
    {
        if (null == chainId || !(DisplayNameMap.ContainsKey(chainId)))
        {
            return;
        }

        obj.DisplayChainName = MustGetChainDisplayName(chainId);
        obj.ChainImageUrl = MustGetChainUrl(chainId);
    }
    public static string GetDisplayName(string chainId)
    {
        return DisplayNameMap.TryGetValue(chainId, out var displayName) 
            ? displayName 
            : chainId;
    }
    
    public static string GetChainUrl(string chainId)
    {
        return ChainImageUrlMap.TryGetValue(chainId, out var chainUrl) 
            ? chainUrl 
            : string.Empty;
    }

    public static void SetDisplayName<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return;
        }

        foreach (object obj in list)
        {
            SetDisplayName(obj);
        }
    }

    private static string GetPropertyValue<T>(T obj, string propertyName)
    {
        if (obj == null || string.IsNullOrEmpty(propertyName))
            return null;

        Type type = obj.GetType();

        PropertyInfo propertyInfo = type.GetProperty(propertyName);

        if (propertyInfo == null)
        {
            return null;
        }

        return propertyInfo.GetValue(obj).ToString();
    }


    private static bool SetPropertyValue<T>(T obj, string propertyName, string value)
    {
        if (obj == null || string.IsNullOrEmpty(propertyName))
            return false;

        Type type = obj.GetType();
        PropertyInfo propertyInfo = type.GetProperty(propertyName);

        if (propertyInfo == null || !propertyInfo.CanWrite)
        {
            return false;
        }

        propertyInfo.SetValue(obj, value);
        return true;
    }
}