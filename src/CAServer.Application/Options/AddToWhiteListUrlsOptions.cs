using System.Collections.Generic;

namespace CAServer.Options;

public class AddToWhiteListUrlsOptions
{
    public List<string> Urls { get; set; }

    public string BaseAddUrl { get; set; }
    public string BaseCheckUrl { get; set; }
}