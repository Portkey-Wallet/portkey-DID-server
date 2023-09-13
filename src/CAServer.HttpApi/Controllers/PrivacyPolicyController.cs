using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("PrivacyPolicy")]
[Microsoft.AspNetCore.Components.Route("api/app/privacypolicy")]
public class PrivacyPolicyController
{
    
}