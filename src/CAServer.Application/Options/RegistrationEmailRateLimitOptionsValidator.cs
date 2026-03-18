using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace CAServer.Options;

public class RegistrationEmailRateLimitOptionsValidator : IValidateOptions<RegistrationEmailRateLimitOptions>
{
    public ValidateOptionsResult Validate(string name, RegistrationEmailRateLimitOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("RegistrationEmailRateLimit options are required.");
        }

        var failures = new List<string>();
        if (options.Policies == null || options.Policies.Count == 0)
        {
            if (options.IsEnabled)
            {
                failures.Add("RegistrationEmailRateLimit:Policies must not be empty when IsEnabled is true.");
            }
        }
        else
        {
            foreach (var (operationType, policy) in options.Policies)
            {
                if (policy == null)
                {
                    failures.Add($"RegistrationEmailRateLimit:Policies:{operationType} is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(policy.GuardianType))
                {
                    failures.Add(
                        $"RegistrationEmailRateLimit:Policies:{operationType}:GuardianType must not be empty.");
                }
                else if (!RegistrationEmailRateLimitGuardianTypeHelper.TryParseDefinedGuardianType(
                             policy.GuardianType, out _))
                {
                    failures.Add(
                        $"RegistrationEmailRateLimit:Policies:{operationType}:GuardianType must be a valid GuardianIdentifierType value.");
                }

                if (policy.Per10Minutes < 0)
                {
                    failures.Add(
                        $"RegistrationEmailRateLimit:Policies:{operationType}:Per10Minutes must be greater than or equal to 0.");
                }

                if (policy.PerHour < 0)
                {
                    failures.Add(
                        $"RegistrationEmailRateLimit:Policies:{operationType}:PerHour must be greater than or equal to 0.");
                }
            }
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
