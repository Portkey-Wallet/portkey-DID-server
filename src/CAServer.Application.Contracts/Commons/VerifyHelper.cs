using System.Text.RegularExpressions;

namespace CAServer.Commons;

public static class VerifyHelper
{
    public static bool VerifyEmail(string address)
    {
        string emailRegex =
            @"([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+";
        var emailReg = new Regex(emailRegex);
        return emailReg.IsMatch(address.Trim());
    }

    public static bool VerifyPhone(string phoneNumber)
    {
        string phoneRegex = @"^1[0-9]{10}$";
        var emailReg = new Regex(phoneRegex);
        return emailReg.IsMatch(phoneNumber.Trim());
    }
}