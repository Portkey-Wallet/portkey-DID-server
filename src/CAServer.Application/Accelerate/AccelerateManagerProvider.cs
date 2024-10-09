using System;
using System.Collections.Generic;
using CAServer.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Accelerate;

public interface IAccelerateManagerProvider
{
    string GenerateOperationDetails(OperationType type, string operationDetailsJson);
}

public class AccelerateManagerProvider : IAccelerateManagerProvider, ISingletonDependency
{
    private readonly AccelerateManagerOptions _accelerateManagerOptions;

    private readonly Dictionary<OperationType, IOperationDetailsProvider> _operationDetailsProviders =
        new Dictionary<OperationType, IOperationDetailsProvider>();

    public AccelerateManagerProvider(IOptions<AccelerateManagerOptions> accelerateManagerOptions,
        IEnumerable<IOperationDetailsProvider> operationDetailsProviders)
    {
        _accelerateManagerOptions = accelerateManagerOptions.Value;
        if (operationDetailsProviders != null)
        {
            foreach (var item in operationDetailsProviders)
            {
                _operationDetailsProviders.Add(item.Type, item);
            }
        }
    }

    public string GenerateOperationDetails(OperationType type, string operationDetailsJson)
    {
        if (type == OperationType.Unknown || operationDetailsJson.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        if (_accelerateManagerOptions.OperationType == null ||
            !_accelerateManagerOptions.OperationType.Contains(Enum.GetName(type)))
        {
            return string.Empty;
        }

        var operationDetailsProvider = _operationDetailsProviders[type];
        if (operationDetailsProvider == null)
        {
            return string.Empty;
        }
        var detailsDto = JsonConvert.DeserializeObject<OperationDetailsDto>(operationDetailsJson);
        return operationDetailsProvider.GenerateOperationDetails(detailsDto);
    }
}