using System;
using CAServer.Commons;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class TimeStampHelperTest
{
    [Fact]
    public void GetTimeStampTest()
    {
        var millisecondsStr = TimeHelper.GetTimeStampInMilliseconds().ToString();
        var secondsStr = TimeHelper.GetTimeStampInSeconds().ToString();
        millisecondsStr.ShouldNotBeNull();
        secondsStr.ShouldNotBeNull();

        var milliseconds = Convert.ToUInt64(millisecondsStr);
        var seconds = Convert.ToUInt64(secondsStr);

        Assert.True(milliseconds > 0);
        Assert.True(seconds > 0);
    }


    [Fact]
    public void TimeParse()
    {
        var time = DateTime.Parse("2023-11-01");
        time.ShouldNotBe(new DateTime());
    }
}