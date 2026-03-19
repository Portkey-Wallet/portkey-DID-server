using System.Threading.Tasks;

namespace CAServer.Verifier;

public interface IVerificationRequestOperationHandler
{
    OperationType OperationType { get; }

    Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context);
}
