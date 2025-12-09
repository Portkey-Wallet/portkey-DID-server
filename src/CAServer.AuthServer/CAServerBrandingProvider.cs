using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace CAServer;

[Dependency(ReplaceServices = true)]
public class CAServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "CAServer";
}
