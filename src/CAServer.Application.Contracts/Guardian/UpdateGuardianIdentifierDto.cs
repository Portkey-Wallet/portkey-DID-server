using System;

namespace CAServer.Guardian;

public class UpdateGuardianIdentifierDto : GuardianIdentifierDto
{
    public Guid UserId { get; set; }
    
    public string UnsetGuardianIdentifierHash { get; set; }
}