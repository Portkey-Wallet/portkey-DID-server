using System.Collections.Generic;
using Orleans;

namespace CAServer.CAAccount.Dtos.Zklogin;

[GenerateSerializer]
public class ZkLoginInfoDto
{
	
	// the type may be changed if we decide to use Poseidon hash function later
	[Id(0)]
	public string IdentifierHash { get; set;}
	
	// salt used to generate the identifier_hash, it has to be 16 bytes
	[Id(1)]
	public string Salt { get; set;}
	
	[Id(2)]
	public string Kid { get; set;}
	
	// the identifier of the circuit
	[Id(3)]
	public string CircuitId { get; set;}
	
	// zk_proof is the serialized zk proof
	[Id(4)]
	public string ZkProof { get; set;}
	
	[Id(5)]
	public List<string> ZkProofPiA { get; set; }
	
	[Id(6)]
	public List<string> ZkProofPiB1 { get; set; }
	
	[Id(7)]
	public List<string> ZkProofPiB2 { get; set; }
	
	[Id(8)]
	public List<string> ZkProofPiB3 { get; set; }
	
	[Id(9)]
	public List<string> ZkProofPiC { get; set; }
	
	// the issuer of the jwt
	[Id(10)]
	public string Issuer { get; set;}
	
	// nonce associated with the jwt
	[Id(11)]
	public string Nonce { get; set;}
	
	// the payload that is used to calculate the nonce, where nonce = hash(serialize(nonce_payload))
	[Id(12)]
	public NoncePayload NoncePayload { get; set;}
	
	[Id(13)]
	public string PoseidonIdentifierHash { get; set; }
}