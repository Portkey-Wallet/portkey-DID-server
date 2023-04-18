using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace CAServer.UserAssets;

public class GetAssetsBase : PagedResultRequestDto
{
    public List<string> CaAddresses { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (CaAddresses == null || CaAddresses.Count == 0 || CaAddresses.Any(string.IsNullOrEmpty))
        {
            yield return new ValidationResult("Invalid CaAddresses input.");
        }
    }
}