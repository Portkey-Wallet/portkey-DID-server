using System.Threading.Tasks;

namespace CAServer.Verifier;

public interface IVerificationRequestOperationDispatcher
{
    Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context);
}
