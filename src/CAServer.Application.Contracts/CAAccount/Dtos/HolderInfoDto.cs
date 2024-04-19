using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Dynamic.Core;
using CAServer.Contacts;
using Volo.Abp;

namespace CAServer.CAAccount.Dtos;

public class HolderInfoDto : IValidatableObject
{
    [MaxLength(16)] public string NickName { get; set; }
    public string Avatar { get; set; }
    public InvitationPermissionsEnum InvitationPermission { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var values = Enum.GetValues(typeof(InvitationPermissionsEnum)).ToDynamicList();
        if (!values.Contains(InvitationPermission))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "InvitationPermission" }
            );
        }

        if (NickName.IsNullOrWhiteSpace() && Avatar.IsNullOrWhiteSpace() &&
            InvitationPermission.ToString().IsNullOrWhiteSpace())
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "NickName", "Avatar", "InvitationPermission" }
            );
        }
    }
}