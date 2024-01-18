using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CAServer.UserAssets;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity.Dtos;

public class GetActivitiesRequestDto : PagedResultRequestDto
{
    public List<CAAddressInfo> CaAddressInfos { get; set; }
    public List<string> TransactionTypes { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CaAddressInfos.IsNullOrEmpty() ||
             CaAddressInfos.Any(info => info.CaAddress.IsNullOrEmpty() || info.ChainId.IsNullOrEmpty()))
        {
            yield return new ValidationResult("Invalid CaAddresses or CaAddressInfos input.");
        }
    }
}