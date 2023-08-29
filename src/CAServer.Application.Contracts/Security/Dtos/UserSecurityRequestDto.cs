using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace CAServer.Security.Dtos;

public class GetTransferLimitListByCaHashAsyncDto : PagedResultRequestDto, IValidatableObject
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