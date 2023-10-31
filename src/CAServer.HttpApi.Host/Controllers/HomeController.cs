using Microsoft.AspNetCore.Mvc;
using Serilog;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers;

public class HomeController : AbpController
{
    readonly IDiagnosticContext _diagnosticContext;

    public HomeController(IDiagnosticContext diagnosticContext)
    {
        _diagnosticContext = diagnosticContext;
    }

    public ActionResult Index()
    {
        _diagnosticContext.Set("ResponseCode",200);
        return Redirect("~/swagger");
    }
}
