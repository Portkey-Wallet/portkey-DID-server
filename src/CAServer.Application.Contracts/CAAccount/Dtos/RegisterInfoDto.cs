using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.CAAccount.Dtos;

public class RegisterInfoDto : IValidatableObject
{
    public string LoginGuardianIdentifier { get; set; }
    public string CaHash { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(LoginGuardianIdentifier) && string.IsNullOrWhiteSpace(CaHash))
        {
            yield return new ValidationResult(
                "Invalid type input.",
                new[] { "LoginGuardianIdentifier, caHash can not both empty or whitespace." }
            );
        }
    }
}