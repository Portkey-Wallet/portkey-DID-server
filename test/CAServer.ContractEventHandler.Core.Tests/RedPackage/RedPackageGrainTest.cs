

using Xunit;

namespace CAServer.ContractEventHandler.Core.Tests.RedPackage;

public class RedPackageHandlerTest :CAServerApplicationTestBase
{
    private readonly PayRedPackageTask _packageTask;

    public RedPackageHandlerTest(PayRedPackageTask packageTask)
    {
        _packageTask = packageTask;
    }

    [Fact]
    public void RedPackageTaskTest()
    {
        _packageTask.PayRedPackageAsync(null);
    }
}