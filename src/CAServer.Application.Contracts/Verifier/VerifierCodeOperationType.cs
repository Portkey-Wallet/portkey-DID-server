namespace CAServer.Verifier;

public enum VerifierCodeOperationType
{
    CreateCAHolder = 0,
    SocialRecovery = 1,
    AddGuardian = 2,
    RemoveGuardian = 3,
    UpdateGuardian = 4,
    RemoveOtherManagerInfo = 5
}