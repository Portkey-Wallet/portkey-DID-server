using System;

namespace CAServer.Growth.constant;

public static class HamsterTonGiftsConstant
{
    
    public const string UserIdsKey = "Hamster:TonGifts:UserIdsKey";
    public const string DoneUserIdsKeyPrefix = "Hamster:TonGifts:UserIdsKey:Done:";
    public const string HeightKey = "Hamster:TonGifts:HeightKeyTdvv";
    private const int KeyExpireDays = 30;
    public static readonly TimeSpan KeyExpire = TimeSpan.FromDays(KeyExpireDays);
    public static readonly int MaxResultCount = 1000;
    public static readonly string StatusCompleted = "completed";

}