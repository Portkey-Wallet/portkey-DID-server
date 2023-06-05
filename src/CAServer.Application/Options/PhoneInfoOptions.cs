using System.Collections.Generic;

namespace CAServer.Options;


public class PhoneInfoOptions
{
    
    public PhoneInfoItem Default { get; set; }

    public List<PhoneInfoItem> PhoneInfo { get; set; }
}

public class PhoneInfoItem
{
    public string Country { get; set; }
    public string Code { get; set; }
    public string Iso { get; set; }
}

