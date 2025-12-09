namespace CAServer.AccountValidator;

public interface IAccountValidator
{
    string Type { get; }

    bool Validate(string account);
}