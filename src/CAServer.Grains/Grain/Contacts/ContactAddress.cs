namespace CAServer.Grains.Grain.Contacts;

[GenerateSerializer]
public class ContactAddress
{
    [Id(0)]
    public string ChainId { get; set; }
    
    [Id(1)]
    public string ChainName { get; set; }
    
    [Id(2)]
    public string Address { get; set; }
}