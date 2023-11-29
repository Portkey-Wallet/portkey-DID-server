using System.IO;

namespace CAServer.Signature;

public class AElfKeyStoreService : IAElfKeyStoreService
{
    public override byte[] DecryptKeyStore(KeyStoreOptions keyStoreOptions)
    {
        using (var file = File.OpenText(keyStoreOptions.KeyStorePath))
        {
            var json = file.ReadToEnd();
            return DecryptKeyStoreFromJson(keyStoreOptions.KeyStorePassword, json);
        }
    }

}