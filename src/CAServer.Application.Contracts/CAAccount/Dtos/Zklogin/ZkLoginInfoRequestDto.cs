namespace CAServer.CAAccount.Dtos.Zklogin;

public class ZkLoginInfoRequestDto
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
    
    //the unique circuit 
    public string CircuitId { get; set; }
    
    //the timestamp of generating zk nonce, used to verify nonce and nonce payload
    public long Timestamp { get; set; }

    public string PoseidonIdentifierHash { get; set; }
}