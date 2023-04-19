using Shouldly;
using Xunit;

namespace CAServer.Device;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class DeviceAppServiceTests : CAServerApplicationTestBase
{
    protected readonly IEncryptionProvider _encryptionProvider;
    
    public DeviceAppServiceTests()
    {
        _encryptionProvider = GetRequiredService<IEncryptionProvider>();
    }
    
    [Fact]
    public void EncryptionProviderTests()
    {
        var str = "test";
        var key = "12345678901234567890123456789012";
        var salt = "1234567890123456";

        var encryptedStr = _encryptionProvider.AESEncrypt(str, key, salt);
        var decryptedStr = _encryptionProvider.AESDecrypt(encryptedStr, key, salt);
        
        decryptedStr.ShouldBe(str);
    }
}