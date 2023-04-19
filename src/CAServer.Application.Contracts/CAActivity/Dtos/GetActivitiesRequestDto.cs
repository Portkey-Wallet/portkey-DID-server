using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace CAServer.CAActivity.Dtos;

public class GetActivitiesRequestDto : PagedResultRequestDto
{
    public List<string> CaAddresses { get; set; }
    public List<string> TransactionTypes { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CaAddresses == null || CaAddresses.Count == 0 || CaAddresses.Any(string.IsNullOrEmpty))
        {
            yield return new ValidationResult("Invalid CaAddresses input.");
        }
    }
}