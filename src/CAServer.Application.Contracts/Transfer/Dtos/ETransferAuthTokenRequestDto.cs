using Microsoft.AspNetCore.Mvc;

namespace CAServer.Transfer.Dtos;

public class ETransferAuthTokenRequestDto
{
    [FromForm(Name = "ca_hash")] public string CaHash { get; set; }
    [FromForm(Name = "chain_id")] public string ChainId { get; set; }
    [FromForm(Name = "managerAddress")] public string ManagerAddress { get; set; }
    [FromForm(Name = "plain_text")] public string PlainText { get; set; }
    [FromForm(Name = "pubkey")] public string Pubkey { get; set; }
    [FromForm(Name = "signature")] public string Signature { get; set; }
    [FromForm(Name = "client_id")] public string ClientId { get; set; }
    [FromForm(Name = "grant_type")] public string GrantType { get; set; }
    [FromForm(Name = "version")] public string Version { get; set; }
    [FromForm(Name = "source")] public string Source { get; set; }
    [FromForm(Name = "scope")]public string Scope { get; set; }
}