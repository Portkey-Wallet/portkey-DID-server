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

public class RemoveOtherManagerInfoOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.RemoveOtherManagerInfo;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.RemoveManager.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return operationDetailsDto.RemoveManager;
    }
}

public class GuardianOperationDetailsHelper
{
    public static string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.IdentifierHash.IsNullOrWhiteSpace() || operationDetailsDto.VerifierId.IsNullOrWhiteSpace() || 
            operationDetailsDto.GuardianType == -1)
        {
            return string.Empty;
        }

        return $"{operationDetailsDto.IdentifierHash}_{operationDetailsDto.GuardianType}_{operationDetailsDto.VerifierId}"; 
    }
}

public class AddGuardianOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.AddGuardian;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        return GuardianOperationDetailsHelper.GenerateOperationDetails(operationDetailsDto);
    }
}

public class RemoveGuardianOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.RemoveGuardian;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        return GuardianOperationDetailsHelper.GenerateOperationDetails(operationDetailsDto);
    }
}

public class SetLoginGuardianOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.SetLoginGuardian;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        return GuardianOperationDetailsHelper.GenerateOperationDetails(operationDetailsDto);
    }
}
public class UnsetLoginGuardianOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.UnSetLoginAccount;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        return GuardianOperationDetailsHelper.GenerateOperationDetails(operationDetailsDto);
    }
}

public class UpdateGuardianOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.UpdateGuardian;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.IdentifierHash.IsNullOrWhiteSpace() || operationDetailsDto.PreVerifierId.IsNullOrWhiteSpace() || 
            operationDetailsDto.NewVerifierId.IsNullOrWhiteSpace() || operationDetailsDto.GuardianType == -1)
        {
            return string.Empty;
        }

        return $"{operationDetailsDto.IdentifierHash}_{operationDetailsDto.GuardianType}_{operationDetailsDto.PreVerifierId}_{operationDetailsDto.NewVerifierId}"; 
    }
}



public class ApproveOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.Approve;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.Spender.IsNullOrWhiteSpace() || operationDetailsDto.Symbol.IsNullOrWhiteSpace() || 
            operationDetailsDto.Amount.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return $"{operationDetailsDto.Spender}_{operationDetailsDto.Symbol}_{operationDetailsDto.Amount}"; 
    }
}

public class TransferOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.GuardianApproveTransfer;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.To.IsNullOrWhiteSpace() || operationDetailsDto.Symbol.IsNullOrWhiteSpace() || 
            operationDetailsDto.Amount.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return $"{operationDetailsDto.To}_{operationDetailsDto.Symbol}_{operationDetailsDto.Amount}"; 
    }
}

public class TransferLimitOperationDetailsProvider : IOperationDetailsProvider, ISingletonDependency
{
    public OperationType Type => OperationType.ModifyTransferLimit;
    public string GenerateOperationDetails(OperationDetailsDto operationDetailsDto)
    {
        if (operationDetailsDto.Symbol.IsNullOrWhiteSpace() || operationDetailsDto.SingleLimit == 0 || 
            operationDetailsDto.DailyLimit == 0)
        {
            return string.Empty;
        }

        return $"{operationDetailsDto.Symbol}_{operationDetailsDto.SingleLimit}_{operationDetailsDto.DailyLimit}"; 
    }
}