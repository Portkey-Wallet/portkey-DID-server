using System;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class TimeStampHelperTest
{
    [Fact]
    public void GetTimeStampTest()
    {
        var millisecondsStr = TimeStampHelper.GetTimeStampInMilliseconds();
        var secondsStr = TimeStampHelper.GetTimeStampInSeconds();
        millisecondsStr.ShouldNotBeNull();
        secondsStr.ShouldNotBeNull();

        var milliseconds = Convert.ToUInt64(millisecondsStr);
        var seconds = Convert.ToUInt64(secondsStr);

        Assert.True(milliseconds > 0);
        Assert.True(seconds > 0);
    }
}