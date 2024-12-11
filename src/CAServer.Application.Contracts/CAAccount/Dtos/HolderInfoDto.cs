using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using CAServer.Contacts;
using Orleans;
using Volo.Abp;

namespace CAServer.CAAccount.Dtos;

[GenerateSerializer]
public class HolderInfoDto : IValidatableObject
{
    [Id(0)]
    [MaxLength(16)] public string NickName { get; set; }
    [Id(1)]
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