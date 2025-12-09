namespace CAServer.Growth.Dtos;

public class ShortLinkDto
{
    public string ShortLink { get; set; }
    public UserGrowthInfo UserGrowthInfo { get; set; }
}

public class UserGrowthInfo
{
    public string CaHash { get; set; }
    public string ProjectCode { get; set; }
    public string InviteCode { get; set; }
    public string ShortLinkCode { get; set; }
}