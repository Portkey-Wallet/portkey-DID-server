using System;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.RedPackage;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

    public static string BuildTransferContent(string content, string toUserName, TransferIndex transfer)
    {
        var customMessage = JsonConvert.DeserializeObject<TransferCustomMessage<TransferCard>>(content);
        customMessage.Data = new TransferCard
        {
            Id = transfer.Id,
            SenderId = transfer.SenderId,
            Memo = transfer.Memo,
            TransactionId = transfer.TransactionId,
            BlockHash = transfer.BlockHash,
            ToUserId = transfer.ToUserId,
            ToUserName = toUserName
        };

        customMessage.TransferExtraData = new TransferExtraData
        {
            Amount = transfer.Amount,
            Decimal = transfer.Decimal,
            Symbol = transfer.Symbol
        };

        return JsonConvert.SerializeObject(customMessage, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
    }
}