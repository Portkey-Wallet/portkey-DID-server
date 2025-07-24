using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tokens.Provider;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace CAServer.Accelerate;

public interface IAccelerateManagerProvider
{
    Task<string> GenerateOperationDetails(OperationType type, string operationDetailsJson);
}

public class AccelerateManagerProvider : IAccelerateManagerProvider, ISingletonDependency
{
    private readonly AccelerateManagerOptions _accelerateManagerOptions;
    private readonly ITokenProvider _tokenProvider;

    private readonly Dictionary<OperationType, IOperationDetailsProvider> _operationDetailsProviders =
        new Dictionary<OperationType, IOperationDetailsProvider>();

    public AccelerateManagerProvider(IOptions<AccelerateManagerOptions> accelerateManagerOptions,
        IEnumerable<IOperationDetailsProvider> operationDetailsProviders,
        ITokenProvider tokenProvider)
    {
        _accelerateManagerOptions = accelerateManagerOptions.Value;
        _tokenProvider = tokenProvider;
        if (operationDetailsProviders != null)
        {
            foreach (var item in operationDetailsProviders)
            {
                _operationDetailsProviders.Add(item.Type, item);
            }
        }
    }

    public async Task<string> GenerateOperationDetails(OperationType type, string operationDetailsJson)
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
        if (type == OperationType.GuardianApproveTransfer)
        {
            detailsDto.ToAddress = AddressHelper.ToShortAddress(detailsDto.ToAddress);
            var tokenInfo = await _tokenProvider.GetTokenInfoAsync(CommonConstant.MainChainId, detailsDto.Symbol);
            detailsDto.Amount = ShiftAmountString(detailsDto.Amount, tokenInfo.Decimals);
        }

        return operationDetailsProvider.GenerateOperationDetails(detailsDto);
    }
    
    private static string ShiftAmountString(string amountStr, int shift)
    {
        if (!decimal.TryParse(amountStr, out var amount))
            throw new ArgumentException("Invalid amount string");

        var multiplier = (decimal)Math.Pow(10, shift);
        var shifted = amount * multiplier;

        return ((long)shifted).ToString(); 
    
    }
}