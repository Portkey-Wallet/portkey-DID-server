using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Providers.ExecuteStrategy;

public class AlchemyPayAuthStrategy : IThirdPartExecuteStrategy<CommonThirdPartExecuteInput, CommonThirdPartExecuteOutput>
{
    
    public ThirdPartExecuteStrategy ExecuteStrategy()
    {
        return ThirdPartExecuteStrategy.AppleAuth;
    }

    public CommonThirdPartExecuteOutput Execute(string secret, CommonThirdPartExecuteInput input)
    {
        throw new System.NotImplementedException();
    }
    
}