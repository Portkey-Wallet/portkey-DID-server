namespace CAServer.SecurityServer.Dtos;

/// <summary>
///     The return result of the general key execution policy,
///     which is applicable to the return result of the simple string type.
/// </summary>
public class CommonThirdPartExecuteOutput
{
    public string Value { get; set; }

    public CommonThirdPartExecuteOutput(string value)
    {
        Value = value;
    }
}

/// <summary>
///     Basic policy entry parameters.
///     Other business entry parameters of the policy are provided by specific subclasses.
/// </summary>
public class BaseThirdPartExecuteInput
{
    public string Key { get; set; }
    
}


/// <summary>
///     A common execution policy
///     for signing a string-type service parameter by using a secret key.
/// </summary>
public class CommonThirdPartExecuteInput : BaseThirdPartExecuteInput
{
    public string BizData { get; set; }
}

/// <summary>
///     A common execution policy
///     for signing a string-type service parameter by using a secret key.
/// </summary>
public class AppleAuthExecuteInput : BaseThirdPartExecuteInput
{
    public string KeyId { get; set; }
    public string TeamId { get; set; }
    public string ClientId { get; set; }
}