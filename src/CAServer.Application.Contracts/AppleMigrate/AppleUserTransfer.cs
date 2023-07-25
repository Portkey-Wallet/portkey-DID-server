using System.Collections.Generic;

namespace CAServer.AppleMigrate;

public class AppleUserTransfer
{
    public List<AppleUserTransferInfo> AppleUserTransferInfos { get; set; } = new();
}

public class AppleUserTransferInfo
{
    /// <summary>
    /// userId in old team
    /// </summary>
    public string UserId { get; set; }
    public string TransferSub { get; set; }
    
    /// <summary>
    /// userId in new team
    /// </summary>
    public string Sub { get; set; }
    public string Email { get; set; }
    public bool IsPrivateEmail { get; set; }
}