using System;
using Shouldly;
using Xunit;

namespace CAServer.Switch;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class SwitchTest : CAServerApplicationTestBase
{
    private readonly ISwitchAppService _switchAppService;

    public SwitchTest()
    {
        _switchAppService = GetRequiredService<ISwitchAppService>();
    }

    [Fact]
    public void GetSwitchStatus_Test()
    {
        var switchName = "ramp";
        var dto = _switchAppService.GetSwitchStatus(switchName);
        dto.ShouldNotBeNull();
        dto.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void GetSwitchStatus_Not_Exist_Test()
    {
        try
        {
            var switchName = "test";
            _switchAppService.GetSwitchStatus(switchName);
        }
        catch (Exception e)
        {
           e.Message.ShouldBe("Switch not exists");
        }
    }
}