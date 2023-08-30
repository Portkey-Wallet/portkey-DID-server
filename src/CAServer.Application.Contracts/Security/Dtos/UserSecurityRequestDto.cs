using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace CAServer.Security.Dtos;

public class GetTransferLimitListByCaHashDto : PagedResultRequestDto, IValidatableObject
{
    [Required] public string CaHash { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrEmpty(CaHash))
        {
            yield return new ValidationResult("Invalid CaHash input.");
        }
    }
}

public class GetManagerApprovedListByCaHashDto : PagedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string CaHash { get; set; }
    public string Spender { get; set; }
    public string Symbol { get; set; }
}