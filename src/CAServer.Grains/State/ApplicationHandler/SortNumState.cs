namespace CAServer.Grains.State.ApplicationHandler;

public class SortNumState
{
    public string Id { get; set; }
    
    public long SortNum { get; set; }
    
    public DateTime ResetTime { get; set; }
}