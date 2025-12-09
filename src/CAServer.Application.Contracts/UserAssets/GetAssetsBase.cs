using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace CAServer.UserAssets;

public class GetAssetsBase : PagedResultRequestDto
{
    public List<CAAddressInfo> CaAddressInfos { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CaAddressInfos.IsNullOrEmpty() ||
            CaAddressInfos.Any(info => info.CaAddress.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty()))
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