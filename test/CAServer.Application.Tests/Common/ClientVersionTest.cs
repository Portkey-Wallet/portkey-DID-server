using CAServer.Commons;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.Common;

public class ClientVersionTest
{

    private ITestOutputHelper _output;

    public ClientVersionTest(ITestOutputHelper testOutputHelper)
    {
        _output = testOutputHelper;
    }


    [Fact]
    public void Test()
    {
        _output.WriteLine((ClientVersion.Of("v1.2.3") == ClientVersion.Of("1.2.3")) + "");
        _output.WriteLine((ClientVersion.Of("v1.2.3") < ClientVersion.Of("1.2.4")) + "");
        _output.WriteLine((ClientVersion.Of("v1.2.3") > ClientVersion.Of("1.2.2")) + "");
    }
    
    
}