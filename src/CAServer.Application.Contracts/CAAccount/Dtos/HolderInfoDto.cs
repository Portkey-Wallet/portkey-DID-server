using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class HolderInfoDto : IValidatableObject
{
    public string NickName { get; set; }
    public string Avatar { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NickName.IsNullOrWhiteSpace() && Avatar.IsNullOrWhiteSpace())
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "NickName", "Avatar" }
            );
        }
    }
}