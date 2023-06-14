using System;

namespace CAVerifierServer.IpWhiteList;

public class AddUserIpToWhiteListRequestDto
{
    public string UserIp { get; set; }
    
    public Guid UserId { get; set; }
}