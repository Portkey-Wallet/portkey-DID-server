using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CAServer.Commons;

namespace CAServer.Telegram.Dtos;

public class RegisterTelegramBotDto : IValidatableObject
{
    [Required]
    public string Secret { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Secret.Contains(CommonConstant.Colon))
        {
            yield return new ValidationResult(
                "Invalid Secret input.",
                new[] { "Secret Format is invalid" }
            );
        }

        var secrets = Secret.Split(CommonConstant.Colon);
        if (secrets.Length != 2)
        {
            yield return new ValidationResult(
                "Invalid Secret format.",
                new[] { "Secret Format is invalid" }
            );
        }
        if (!Regex.IsMatch(secrets[0], @"^\d+$"))
        {
            yield return new ValidationResult(
                "Invalid Secret botId.",
                new[] { "Secret Format is invalid, botId is invalid" }
            );
        }

        if (!Regex.IsMatch(secrets[1], @"^[a-zA-Z0-9_-]+$"))
        {
            yield return new ValidationResult(
                "Invalid Secret.",
                new[] { "Secret Content Format is invalid" }
            );
        }
    }
}