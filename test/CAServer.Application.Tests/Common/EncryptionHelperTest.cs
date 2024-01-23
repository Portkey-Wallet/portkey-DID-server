using CAServer.Commons;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.Common;

public class EncryptionHelperTest : CAServerApplicationTestBase
{

    public EncryptionHelperTest(ITestOutputHelper output) : base(output)
    {
    }
    

    [Fact]
    public void Test()
    {

        var key = "123";
        var data = "This is a test data";

        var encryptData = EncryptionHelper.EncryptBase64(data, key);
        var encryptData2 = EncryptionHelper.EncryptHex(data, key);

        var decryptData = EncryptionHelper.DecryptFromBase64(encryptData, key);
        var decryptData2 = EncryptionHelper.DecryptFromHex(encryptData2, key);
        decryptData.ShouldBe(data);
        decryptData2.ShouldBe(data);
        

    }

}