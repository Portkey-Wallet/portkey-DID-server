namespace CAServer.Verifier;

public enum VerifierCodeOperationType
{
    Unknown = 0,
    CreateCAHolder = 1,
    SocialRecovery = 2,
    AddGuardian = 3,
    RemoveGuardian = 4,
    UpdateGuardian = 5,
    RemoveOtherManagerInfo = 6
}