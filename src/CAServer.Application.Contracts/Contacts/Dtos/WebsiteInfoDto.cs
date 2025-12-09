using System.Collections.Generic;

namespace CAServer.Contacts;

public class WebsiteInfoDto
{
    public string Website { get; set; }

    public string Logo { get; set; }

    public List<string> Spenders { get; set; }
}