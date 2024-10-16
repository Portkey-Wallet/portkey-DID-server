using System;
using System.Collections.Generic;
using System.Reflection;

namespace CAServer.Commons;

public static class ChainDisplayNameHelper
{
    public static readonly string MainChain = "aelf MainChain";
    public static readonly string DAppChain = "aelf dAppChain";

    public static Dictionary<string, string> DisplayNameMap = new Dictionary<string, string>
    {
        { "AELF", MainChain },
        { "tDVV", DAppChain },
        { "tDVW", DAppChain },
    };

    // if not exits, maybe throw exception
    public static string MustGetChainDisplayName(string chainId)
    {
        return DisplayNameMap[chainId];
    }

    // if not exits, return default
    public static string GetChainDisplayNameByDefault(string chainId)
    {
        return DisplayNameMap.GetValueOrDefault(chainId, DAppChain);
    }

    public static bool SetDisplayName<T> (T obj)
    {
        string chainId = GetPropertyValue(obj, "ChainId");
        if (null == chainId)
        {
            return false;
        }

        string displayName = MustGetChainDisplayName(chainId);
        return SetPropertyValue(obj, "DisplayName", displayName);
    }

    public static bool SetDisplayName<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return false;
        }

        bool result = true;
        foreach (object obj in list)
        {
            if (!SetDisplayName(obj))
            {
                result = false;
            }
        }

        return result;
    }

    private static string GetPropertyValue<T> (T obj, string propertyName)
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


    private static bool SetPropertyValue<T> (T obj, string propertyName, string value)
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