namespace CAServer.Monitor;

public class Indicator
{
    public string Application { get; set; }
    public string Module { get; set; }
    public string Tag { get; set; }
    public string Target { get; set; }
    public int Value { get; set; }
}