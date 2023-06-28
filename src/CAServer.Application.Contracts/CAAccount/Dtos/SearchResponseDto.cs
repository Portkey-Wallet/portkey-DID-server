namespace CAServer.CAAccount.Dtos;

public class SearchResponseDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public long Balance { get; set; }
}