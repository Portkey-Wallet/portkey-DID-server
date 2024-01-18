using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace CAServer.ThirdPart.Dtos.ThirdPart;

public class TreasuryBaseContext
{
    [CanBeNull] public HttpContext HttpContext { get; set; }
    
}