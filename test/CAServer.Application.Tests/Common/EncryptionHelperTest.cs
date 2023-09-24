using Shouldly;
using Xunit;

namespace CAServer.Common;

public class EncryptionHelperTest
{

    [Fact]
    public void Test()
    {

        var key = "123";
        var data = "This is a test data";

        var encryptData = EncryptionHelper.Encrypt(data, key);
        var encryptData2 = EncryptionHelper.Encrypt(data, key);
        encryptData.ShouldNotBe(encryptData2);

        var decryptData = EncryptionHelper.Decrypt(encryptData, key);
        var decryptData2 = EncryptionHelper.Decrypt(encryptData2, key);
        decryptData.ShouldBe(data);
        decryptData2.ShouldBe(data);
        

    }
    
}