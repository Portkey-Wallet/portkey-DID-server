using System.Text.RegularExpressions;


namespace CAServer.AccountValidator;

public class PhoneValidator : IAccountValidator
{
    public string Type => "PhoneNumber";
    private readonly Regex _regex;

    public PhoneValidator()
    {
        _regex = new Regex(CAServerApplicationConsts.PhoneRegex);
    }
    
    public bool Validate(string account)
    {
        return !string.IsNullOrWhiteSpace(account) && _regex.IsMatch(account);
    }
}