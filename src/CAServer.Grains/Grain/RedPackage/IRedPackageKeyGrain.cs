using Orleans;

namespace CAServer.Grains.Grain.RedPackage;

public interface IRedPackageKeyGrain : IGrainWithGuidKey
{
    Task<string> GenerateKey();
    Task<string> GenerateSignature(string input);
    Task<bool> VerifySignature(string input,string sig);
    Task<string> GetPublicKey();
}