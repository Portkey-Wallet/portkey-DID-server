using System;
using System.Text.RegularExpressions;

namespace CAServer.Commons;

public static class AppleHelper
{
    private const string AppleUserRegex = @"^([0-9]+\.)([0-9a-z]+\.)([0-9]+)$";
    
    public static bool IsAppleUserId(string userId)
    {
        if (userId.IsNullOrWhiteSpace()) return false;
        
        var regex = new Regex(AppleUserRegex);
        return regex.IsMatch(userId);
    }
}