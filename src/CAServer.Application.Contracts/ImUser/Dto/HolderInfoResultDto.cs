using System;
using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.ImUser.Dto;

public class HolderInfoResultDto
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
    public string Avatar { get; set; }
    public List<AddressInfoDto> AddressInfos { get; set; }
}

public class AddressInfoDto : ChainDisplayNameDto
{
    public string Address { get; set; }
}