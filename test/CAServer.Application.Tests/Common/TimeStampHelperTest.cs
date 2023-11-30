using System;
using CAServer.Commons;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.Common;

public class TimeStampHelperTest
{

    private ITestOutputHelper _testOutputHelper;

    public TimeStampHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }


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

    [Fact]
    public void TimeString()
    {
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(-1));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToUtcString());
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(0));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(1));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(2));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(3));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(4));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(5));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(6));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(7));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToZoneString(8));
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToUtc8String());
        _testOutputHelper.WriteLine(DateTime.UtcNow.ToUtc8String("yyyy-MM-dd"));
        _testOutputHelper.WriteLine("");
        
        _testOutputHelper.WriteLine(TimeHelper.ParseFromZone("2023-10-10", 8, "yyyy-MM-dd").ToUtcString());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromZone("2023-10-10", 8, "yyyy-MM-dd").ToUtc8String());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromZone("2023-10-10 10:01:02", 8).ToUtcString());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromZone("2023-10-10 10:01:02", 8).ToUtc8String());
        
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine(TimeHelper.ParseFromUtcString("2023-11-03T07:30:03.0154358Z").ToUtc8String());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromUtcString("2023-11-03T07:30:03.011Z").ToUtcString());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromUtcString("2023-11-03T07:30:03.011Z").ToUtc8String());
        _testOutputHelper.WriteLine(TimeHelper.ParseFromUtcString("2023-11-03T07:30:03.011Z").ToUtcString());
    }
    
}