using System.Collections.Generic;

namespace CAServer.Options;

public class FireBaseAppCheckOptions
{
    
    public string RequestUrl { get; set; }
    
    public string ValidIssuer { get; set; }
    
    public List<string> ValidAudiences { get; set; }
    
    public string Sub { get; set; }
    
    
    
    
}