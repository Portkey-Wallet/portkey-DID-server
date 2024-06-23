using System;

namespace CAServer.EntityEventHandler.Core.Worker;

public class WorkerConst
{
    public const int TimePeriod = 3000;
    public const int MaxOlderBlockHeightFromNow = 100000;
    public const int CryptoGiftExpiredTimePeriod = 60000;
    //public const int InitReferralTimePeriod = Int32.MaxValue;
    public const int InitReferralTimePeriod = 30000;
    public const int ReferralTimePeriod = 20000;
}