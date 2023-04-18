using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class ValidAddressesAttribute : ValidationAttribute
{
    private static int MaxAddressesCount { get; set; } = 5;

    protected override ValidationResult IsValid(
        object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult(GetErrorMessage(validationContext.DisplayName));
        }

        var asddresses = value as List<ContactAddressDto>;

        if (asddresses == null || asddresses.Count == 0)
        {
            return new ValidationResult(
                $"{validationContext.DisplayName} can not be null or empty!",
                new[] { validationContext.DisplayName });
        }

        if (asddresses.Count > MaxAddressesCount)
        {
            return new ValidationResult(
                "Too many addresses!",
                new[] { validationContext.DisplayName });
        }

        return ValidationResult.Success;
    }

    private string GetErrorMessage(string element)
    {
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
            return ErrorMessage;

        return $"Invalid input {element}.";
    }
}