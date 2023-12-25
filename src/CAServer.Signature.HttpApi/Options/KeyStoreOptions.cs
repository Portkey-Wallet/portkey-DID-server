using System.Collections.Generic;

namespace SignatureServer.Options;

public class KeyStoreOptions
{
    public string Path { get; set; }
    public Dictionary<string, string> Passwords { get; set; } = new();
    public List<string> LoadAddress { get; set; } = new();
}
