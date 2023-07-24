using System.Collections.Generic;
using System.Text.RegularExpressions;
using CAServer.Commons;

namespace CAServer.Options;

public class AppleTransferOptions
{
    private const string AppleUserRegex = @"^([0-9]+\.)([0-9a-z]+\.)([0-9]+)$";
    public bool CloseLogin { get; set; }
    public List<string> WhiteList { get; set; } = new();
    public string ErrorMessage { get; set; } = CommonConstant.AppleTransferMessage;

    public bool IsNeedIntercept(string userId)
    {
        if (!CloseLogin) return false;

        var regex = new Regex(AppleUserRegex);
        if (!regex.IsMatch(userId)) return false;

        return !WhiteList.Contains(userId);
    }
}