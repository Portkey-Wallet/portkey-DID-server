using Volo.Abp.Settings;

namespace CAServer.Settings;

public class CAServerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(CAServerSettings.MySetting1));
    }
}
