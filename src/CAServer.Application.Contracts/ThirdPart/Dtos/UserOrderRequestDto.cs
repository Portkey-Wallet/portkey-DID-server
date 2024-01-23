using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CAServer.Commons;
using Volo.Abp.Application.Dtos;

namespace CAServer.ThirdPart.Dtos;

public class CreateUserOrderDto : IValidatableObject
{
    // UserId Only available in test sessions since we don't' get authorized user. 
    public Guid UserId { get; set; }
    [Required] public string TransDirect { get; set; }
    public string MerchantName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MerchantName != null && ThirdPartHelper.MerchantNameExist(MerchantName) == ThirdPartNameType.Unknown)
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
    public Guid OrderId { get; set; }
}

public class GetThirdPartOrderConditionDto : PagedResultRequestDto
{

    public GetThirdPartOrderConditionDto()
    {
        
    }
    
    public GetThirdPartOrderConditionDto(int skipCount, int maxResultCount)
    {
        base.SkipCount = skipCount;
        base.MaxResultCount = maxResultCount;
    }

    public Guid UserId { get; set; }
    
    // string type of millisecond long value
    public string LastModifyTimeLt { get; set; }
    public string LastModifyTimeGt { get; set; }
    public List<Guid> OrderIdIn { get; set; }
    
    public string ThirdPartName { get; set; }
    public List<string> ThirdPartOrderNoIn { get; set; }
    
    /// <see cref="TransferDirectionType"/>
    public List<string> TransDirectIn { get; set; }
    
    /// <see cref="OrderStatusType"/>
    public List<string> StatusIn { get; set; }
    
    public string TransactionId { get; set; }
}