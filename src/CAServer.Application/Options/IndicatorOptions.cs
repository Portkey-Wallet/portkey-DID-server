using CAServer.Commons;

namespace CAServer.Options;

public class IndicatorOptions
{
    public string Application { get; set; } = CommonConstant.ApplicationName;
    public string Module { get; set; } = CommonConstant.ModuleName;
}