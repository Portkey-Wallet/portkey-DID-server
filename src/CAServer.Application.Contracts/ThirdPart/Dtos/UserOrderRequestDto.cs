using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.Commons;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart.Dtos;

public class CreateUserOrderDto : IValidatableObject
{
    public string OrderId { get; set; }
    // UserId Only available in test sessions since we don't' get authorized user. 
    public Guid UserId { get; set; }
    [Required] public string TransDirect { get; set; }
    [Required] public string MerchantName { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (ThirdPartHelper.MerchantNameExist(MerchantName) == MerchantNameType.Unknown)
        {
            yield return new ValidationResult(
                $"Merchant name {MerchantName} is not exist."
            );
        }

        if (!ThirdPartHelper.TransferDirectionTypeExist(TransDirect))
        {
            yield return new ValidationResult(
                $"Transfer direction {TransDirect} is not available."
            );
        }
    }
}

public class GetUserOrdersDto : PagedResultRequestDto
{
    public Guid UserId { get; set; }
}