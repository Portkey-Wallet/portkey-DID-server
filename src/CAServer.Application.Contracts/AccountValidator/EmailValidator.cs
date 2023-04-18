using System.Text.RegularExpressions;
using System.Transactions;


namespace CAServer.AccountValidator;

public class EmailValidator : IAccountValidator
{
    public string Type => "Email";
    private readonly Regex _regex;

    public EmailValidator()
    {
        _regex = new Regex(CAServerApplicationConsts.EmailRegex);
    }
    
    public bool Validate(string account)
    {
        return !string.IsNullOrWhiteSpace(account) && _regex.IsMatch(account);
    }
}