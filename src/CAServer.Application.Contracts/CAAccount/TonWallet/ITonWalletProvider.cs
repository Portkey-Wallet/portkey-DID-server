using System.Threading;
using System.Threading.Tasks;
using TonProof.Types;

namespace CAServer.CAAccount.TonWallet;

public interface ITonWalletProvider
{
    public Task<VerifyResult> VerifyAsync(CheckProofRequest request, CancellationToken cancellationToken = default);
    
    public string GetTonWalletMessage(CheckProofRequest request);
}