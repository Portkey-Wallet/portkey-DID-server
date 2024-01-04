using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CAServer.UserAssets;

namespace CAServer.CAActivity.Dto;

public class GetActivityRequestDto : IValidatableObject
{
    [Required] public string TransactionId { get; set; }
    [Required] public string BlockHash { get; set; }
    public string ActivityType { get; set; }

    public List<string> CaAddresses { get; set; }

    public List<CAAddressInfo> CaAddressInfos { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(TransactionId))
        {
            yield return new ValidationResult("Invalid TransactionId input.");
        }

        if (string.IsNullOrEmpty(BlockHash))
        {
            yield return new ValidationResult("Invalid BlockHash input.");
        }

        if ((CaAddressInfos.IsNullOrEmpty() ||
             CaAddressInfos.Any(info => info.CaAddress.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty())) &&
            (CaAddresses == null || CaAddresses.Count == 0 || CaAddresses.Any(string.IsNullOrEmpty)))
        {
            yield return new ValidationResult("Invalid CaAddresses or CaAddressInfos input.");
        }
    }
}