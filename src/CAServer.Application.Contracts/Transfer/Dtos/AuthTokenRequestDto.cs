using Microsoft.AspNetCore.Mvc;

namespace CAServer.Transfer.Dtos;

public class AuthTokenRequestDto
{
    [FromForm(Name = "ca_hash")] public string CaHash { get; set; }
    [FromForm(Name = "chain_id")] public string ChainId { get; set; }
    public string ManagerAddress { get; set; }
    [FromForm(Name = "plain_text")] public string PlainText { get; set; }
    public string Pubkey { get; set; }
    public string Signature { get; set; }
}