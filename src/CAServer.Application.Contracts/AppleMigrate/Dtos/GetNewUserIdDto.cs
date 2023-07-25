namespace CAServer.AppleMigrate.Dtos;

public class GetNewUserIdDto
{
    public string Sub { get; set; }
    public string Email { get; set; }
    public bool Is_private_email { get; set; }
}