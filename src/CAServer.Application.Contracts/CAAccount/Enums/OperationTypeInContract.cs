namespace CAServer.CAAccount.Enums;

public enum OperationTypeInContract
{
    Unknown = 0,
    CreateCAHolder = 1,
    SocialRecovery = 2,
    AddGuardian = 3,
    RemoveGuardian = 4,
    UpdateGuardian = 5,
    RemoveOtherManagerInfo = 6,
    SetLoginAccount = 7,
    Approve = 8,
    ModifyTransferLimit = 9,
    GuardianApproveTransfer = 10,
    UnSetLoginAccount = 11,
}