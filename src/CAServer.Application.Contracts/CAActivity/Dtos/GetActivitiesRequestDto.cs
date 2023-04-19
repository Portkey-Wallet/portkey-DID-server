using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CAServer.UserAssets;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity.Dtos;

public class GetActivitiesRequestDto : PagedResultRequestDto
{
    public List<string> CaAddresses { get; set; }
    public List<CAAddressInfo> CaAddressInfos { get; set; }
    public List<string> TransactionTypes { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // CaAddressInfos and CaAddresses cannot be empty at the same time
        if ((CaAddressInfos == null || CaAddressInfos.Count == 0 ||
             CaAddressInfos.Any(info => info.CaAddress.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty())) &&
            (CaAddresses == null || CaAddresses.Count == 0 || CaAddresses.Any(string.IsNullOrEmpty)))
        {
            yield return new ValidationResult("Invalid CaAddresses or CaAddressInfos input.");
        }
    }
}