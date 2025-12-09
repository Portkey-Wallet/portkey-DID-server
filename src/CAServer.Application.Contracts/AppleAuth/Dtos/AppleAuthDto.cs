using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.AppleAuth.Dtos;

public class AppleAuthDto : IValidatableObject
{
    public string Code { get; set; }
    public string Id_token { get; set; }
    public string State { get; set; }
    public string User { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Id_token) && string.IsNullOrWhiteSpace(Code))
        {
            yield return new ValidationResult(
                "Invalid input.",
                new[] { "id_token" }
            );
        }
    }
    
}

public class AppleExtraInfo
{
    public AppleNameInfo Name { get; set; }
    public string Email { get; set; }
}

public class AppleNameInfo
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}