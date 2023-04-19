using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace CAServer.UserAssets;

public class GetAssetsBase : PagedResultRequestDto
{
    public List<string> CaAddresses { get; set; }
    public List<CAAddressInfo> CaAddressInfos { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
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

public class CAAddressInfo
{
    public string CaAddress { get; set; }
    public string ChainId { get; set; }
}