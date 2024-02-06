using System.Collections.Generic;
using JetBrains.Annotations;

namespace CAServer.ThirdPart;

public class OrderStatusExtensionBuilder
{
    private readonly Dictionary<string, string> _data;

    private OrderStatusExtensionBuilder([CanBeNull] Dictionary<string, string> initData)
    {
        _data = initData ?? new Dictionary<string, string>();
    }

    public Dictionary<string, string> Build()
    {
        return _data;
    }
    
    public static OrderStatusExtensionBuilder Create(Dictionary<string, string> initData = null)
    {
        return new OrderStatusExtensionBuilder(initData);
    }

    public OrderStatusExtensionBuilder Add(string key, string value)
    {
        _data[key] = value;
        return this;
    }
}

public static class ExtensionKey
{
    public const string TxHash = "txHash";
    public const string TxStatus = "txStatus";
    public const string TxResult = "txResult";
    public const string Transaction = "transaction";
    public const string TxBlockHeight = "txBlockHeight";

    public const string ChainHeight = "chainHeight";
    public const string ChainLib = "chainLib";

    public const string CallBackStatus = "callBackStatus";
    public const string CallBackResult = "callBackResult";

    public const string Reason = "Reason";
    public const string AdminUserId = "AdminUserId";
    public const string AdminUserName = "AdminUserName";
    public const string Version = "Version";


}