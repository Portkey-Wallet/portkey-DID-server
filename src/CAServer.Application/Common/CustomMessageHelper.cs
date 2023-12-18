using System;
using CAServer.Commons;
using CAServer.Entities.Es;
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

    public static string BuildTransferContent(string content, TransferIndex transfer)
    {
        var customMessage = JsonConvert.DeserializeObject<TransferCustomMessage<TransferCard>>(content);
        customMessage.Data = new TransferCard
        {
            Id = transfer.Id,
            SenderId = transfer.SenderId,
            Memo = transfer.Memo,
            TransactionId = transfer.TransactionId,
            BlockHash = transfer.BlockHash
        };

        customMessage.TransferExtraData = new TransferExtraData
        {
            Amount = transfer.Amount,
            Decimal = transfer.Decimal,
            Symbol = transfer.Symbol
        };

        return JsonConvert.SerializeObject(customMessage);
    }
}