using Nethereum.KeyStore;

namespace CAServer.Signature;

public abstract class IAElfKeyStoreService : KeyStoreService
{
    public abstract byte[] DecryptKeyStore(KeyStoreOptions keyStoreOptions);
}