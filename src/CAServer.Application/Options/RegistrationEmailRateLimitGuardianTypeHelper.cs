using System;
using CAServer.CAAccount.Dtos;

namespace CAServer.Options;

public static class RegistrationEmailRateLimitGuardianTypeHelper
{
    public static bool TryParseDefinedGuardianType(string guardianType, out GuardianIdentifierType parsedGuardianType)
    {
        parsedGuardianType = default;
        if (string.IsNullOrWhiteSpace(guardianType))
        {
            return false;
        }

        var normalizedGuardianType = guardianType.Trim();
        return Enum.TryParse(normalizedGuardianType, true, out parsedGuardianType) &&
               Enum.IsDefined(typeof(GuardianIdentifierType), parsedGuardianType);
    }
}
