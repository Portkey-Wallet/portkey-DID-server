namespace CAServer.Verifier;

public enum VerifierCodeOperationType
{
    CreateCAHolder = 1,
    SocialRecovery = 2,
    AddGuardian = 3,
    RemoveGuardian = 4,
    UpdateGuardian = 5,
    RemoveOtherManagerInfo = 6
}