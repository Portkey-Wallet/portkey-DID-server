using System;

namespace CAServer.Admin.Dtos;

public class AdminUserResponse
{
    
    
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Rules { get; set; }
    public bool MfaExists { get; set; }


}