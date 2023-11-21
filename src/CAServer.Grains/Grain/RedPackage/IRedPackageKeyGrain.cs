using Orleans;

namespace CAServer.Grains.Grain.RedPackage;

public interface IRedPackageKeyGrain : IGrainWithGuidKey
{
    Task<string> GenerateKey();
    Task<string> GenerateSignature(string input);
    Task<string> GetPublicKey();
}