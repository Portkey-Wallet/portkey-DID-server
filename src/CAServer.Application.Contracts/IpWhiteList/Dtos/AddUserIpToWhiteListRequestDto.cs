using System;

namespace CAServer.IpWhiteList.Dtos;

public class AddUserIpToWhiteListRequestDto
{
    public string UserIp { get; set; }
    
    public Guid UserId { get; set; }
}