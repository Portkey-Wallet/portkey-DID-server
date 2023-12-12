using System;
using CAServer.Commons;
using CAServer.RedPackage;
using Newtonsoft.Json;

namespace CAServer.Common;

public class CustomMessageHelper
{
    public static string BuildRedPackageCardContent(Guid senderId,string memo, Guid redPackageId)
    {
        var redPackageCard = new RedPackageCard
        {
            Id = redPackageId,
            SenderId = senderId,
            Memo = memo
        };
        var result = new CustomMessage<RedPackageCard>();
        result.Data = redPackageCard;
        return JsonConvert.SerializeObject(result);
    }
}