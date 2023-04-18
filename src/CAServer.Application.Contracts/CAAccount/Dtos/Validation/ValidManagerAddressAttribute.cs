using System;
using System.ComponentModel.DataAnnotations;
using AElf;

namespace CAServer.Dtos;

public class ValidManagerAddressAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(
        object value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult(GetErrorMessage(validationContext.DisplayName));
        }

        var managerAddress = value.ToString();
        try
        {
            if (!Base58CheckEncoding.Verify(managerAddress))
            {
                return new ValidationResult(GetErrorMessage(validationContext.DisplayName));
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult(GetErrorMessage(validationContext.DisplayName));
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