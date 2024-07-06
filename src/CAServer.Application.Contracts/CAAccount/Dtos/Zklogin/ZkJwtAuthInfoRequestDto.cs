namespace CAServer.CAAccount.Dtos.Zklogin;

public class ZkJwtAuthInfoRequestDto
{
    // the type may be changed if we decide to use Poseidon hash function later
    public string IdentifierHash { get; set;}
	
    // salt used to generate the identifier_hash, it has to be 16 bytes
    public string Salt { get; set;}
	
    // raw jwt in base64url format
    public string Jwt { get; set;}
    
    // zk_proof is the serialized zk proof
    public string ZkProof { get; set;}
	
    // nonce associated with the jwt
    public string Nonce { get; set;}
}