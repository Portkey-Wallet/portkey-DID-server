using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CAServer.Commons;

namespace CAServer.ThirdPart.Dtos;

public class TreasuryOrderExportResponseDto
{
    public List<TreasuryOrderDto> OrderList { get; set; }

    public TreasuryOrderExportResponseDto(List<TreasuryOrderDto> orders)
    {
        OrderList = orders;
    }


    public string ToCsvString(int timeZone = 0)
    {
        const int DefaultDecimal = 6;
        var csvBuilder = new StringBuilder();
        var stringWriter = new StringWriter(csvBuilder);
        stringWriter.WriteLine(string.Join(CommonConstant.Comma,
            "Id",
            "RampOrderId",
            "OrderType",
            "Status",

            // settlement
            "SettlementAmount_USDT",
            "FeeInfo",
            "TotalFeeAmount_USDT",
            "CryptoExchangeToUSDT",

            // currencies
            "Crypto",
            "CryptoAmount",
            "Fiat",
            "FiatAmount",

            // thirdPart
            "ThirdPartName",
            "ThirdPartOrderId",
            "ThirdPartCallBackTime",

            // transaction
            "UserAddress",
            "TransactionId",
            "TransactionTime",

            // times
            "CreateTime",
            "LastModifyTime"
        ));

        foreach (var order in OrderList)
        {
            var feeInfo = order.FeeInfo.Select(fee => fee.Amount + fee.Symbol).ToList();
            var feeInUsdt = order.FeeInfo
                .Select(fee => fee.Amount.SafeToDecimal() * fee.SymbolPriceInUsdt.SafeToDecimal()).Sum();
            var cryptoExchange = order.TokenExchanges.Where(ex => ex.FromSymbol == order.Crypto)
                .FirstOrDefault(ex => ex.ToSymbol == CommonConstant.USDT);

            stringWriter.WriteLine(string.Join(CommonConstant.Comma,
                order.Id.ToString(),
                order.RampOrderId.ToString(),
                order.TransferDirection,
                order.Status,

                // settlement
                order.SettlementAmount,
                string.Join(CommonConstant.Comma, feeInfo),
                feeInUsdt.ToString(DefaultDecimal),
                cryptoExchange?.Exchange == null
                    ? CommonConstant.EmptyString
                    : cryptoExchange.Exchange.ToString(DefaultDecimal),

                // currencies
                order.Crypto,
                order.CryptoAmount,
                order.Fiat,
                order.FiatAmount,

                // thirdPart
                order.ThirdPartName,
                order.ThirdPartOrderId,
                order.CallbackTime == 0
                    ? CommonConstant.EmptyString
                    : TimeHelper.GetDateTimeFromTimeStamp(order.CallbackTime).ToZoneString(timeZone),

                // addresses
                order.ToAddress,
                order.TransactionId,
                order.TransactionTime == 0
                    ? CommonConstant.EmptyString
                    : TimeHelper.GetDateTimeFromTimeStamp(order.TransactionTime).ToZoneString(timeZone),

                // times
                order.CreateTime == 0
                    ? CommonConstant.EmptyString
                    : TimeHelper.GetDateTimeFromTimeStamp(order.CreateTime).ToZoneString(timeZone),
                order.LastModifyTime == 0
                    ? CommonConstant.EmptyString
                    : TimeHelper.GetDateTimeFromTimeStamp(order.LastModifyTime).ToZoneString(timeZone)
            ));
        }

        stringWriter.Flush();
        return csvBuilder.ToString();
    }
}