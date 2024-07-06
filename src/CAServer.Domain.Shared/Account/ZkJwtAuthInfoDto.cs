namespace CAServer.CAAccount.Dtos.Zklogin;

public class ZkJwtAuthInfoDto
{
	// the type may be changed if we decide to use Poseidon hash function later
	public string IdentifierHash { get; set;}
	
	// salt used to generate the identifier_hash, it has to be 16 bytes
	public string Salt { get; set;}
	
	public string Kid { get; set;}
	
	// the identifier of the circuit
	public string CircuitId { get; set;}
	
	// zk_proof is the serialized zk proof
	public string ZkProof { get; set;}
	
	// the issuer of the jwt
	public string Issuer { get; set;}
	
	// nonce associated with the jwt
	public string Nonce { get; set;}
	
	// the payload that is used to calculate the nonce, where nonce = hash(serialize(nonce_payload))
	public NoncePayload NoncePayload { get; set;}
}