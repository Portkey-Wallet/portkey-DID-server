namespace SignatureServer.Dtos;

/// <summary>
///     Basic policy entry parameters.
///     Only the policy type is defined.
///     Other business entry parameters of the policy are provided by specific subclasses.
/// </summary>
public class BaseThirdPartExecuteInput
{

    public string ExecuteStrategy { get; set; }
    
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
///     The basic return result of the key execution policy.
///     The specific return value is defined in the business subclass.
/// </summary>
public class BaseThirdPartExecuteOutput
{
    
}

/// <summary>
///     The return result of the general key execution policy,
///     which is applicable to the return result of the simple string type.
/// </summary>
public class CommonThirdPartExecuteOutput
{
    public string Value { get; set; }
}