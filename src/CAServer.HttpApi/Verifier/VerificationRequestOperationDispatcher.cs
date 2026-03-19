using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CAServer.Verifier;

public class VerificationRequestOperationDispatcher : IVerificationRequestOperationDispatcher
{
    private readonly IReadOnlyDictionary<OperationType, IVerificationRequestOperationHandler> _handlers;

    public VerificationRequestOperationDispatcher(IEnumerable<IVerificationRequestOperationHandler> handlers)
    {
        _handlers = handlers.ToDictionary(x => x.OperationType);
    }

    public async Task<VerificationRequestOperationResult> HandleAsync(VerificationRequestOperationContext context)
    {
        if (!_handlers.TryGetValue(context.OperationType, out var handler))
        {
            return VerificationRequestOperationResult.NotHandled();
        }

        return await handler.HandleAsync(context);
    }
}
