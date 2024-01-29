using System;
using CAServer.Verifier;
using Volo.Abp.DependencyInjection;

namespace CAServer.Accelerate;

public interface IOperationDetailsProvider
{
    OperationType Type { get;}
    string GenerateOperationDetails(OperationDetailsDto operationDetailsDto);
}

public class CreateCaHolderOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.CreateCAHolder;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.Manager.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return operationDetailsDto.Manager;
    }
}

public class SocialRecoveryOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.SocialRecovery;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.Manager.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return operationDetailsDto.Manager;
    }
}