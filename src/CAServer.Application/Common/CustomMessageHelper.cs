using System;
using CAServer.Commons;
using CAServer.RedPackage;
using Newtonsoft.Json;

namespace CAServer.Common;

public class CustomMessageHelper
{
    public static string BuildRedPackageCardContent(Guid senderId, string memo, Guid redPackageId)
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

    public static string BuildTransferContent(string content, Guid senderId, string memo, string transferId,
        string transactionId, string blockHash)
    {
        var customMessage = JsonConvert.DeserializeObject<CustomMessage<TransferCard>>(content);
        customMessage.Data = new TransferCard
        {
            Id = transferId,
            SenderId = senderId,
            Memo = memo,
            TransactionId = transactionId,
            BlockHash = blockHash
        };

        return JsonConvert.SerializeObject(customMessage);
    }
}