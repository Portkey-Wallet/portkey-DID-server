using System;
using System.Collections.Generic;
using CAServer.ThirdPart.Dtos;
using System.Security.Cryptography;
using System.Text;

namespace CAServer.ThirdPart;

public static class ThirdPartHelper
{
    public static MerchantNameType MerchantNameExist(string merchantName)
    {
        var match = Enum.TryParse(merchantName, true, out MerchantNameType matched);
        return match ? matched : MerchantNameType.Unknown;
    }

    public static bool TransferDirectionTypeExist(string transferDirectionType)
    {
        return Enum.TryParse(transferDirectionType, true, out TransferDirectionType _);
    }

    public static bool ValidateMerchantOrderNo(string merchantOrderNo)
    {
        return merchantOrderNo.IsNullOrEmpty() || Guid.TryParse(merchantOrderNo, out Guid _);
    }

    public static Guid GetOrderId(string merchantOrderNo)
    {
        Guid.TryParse(merchantOrderNo, out Guid orderNo);
        return orderNo;
    }
    
    public static Guid GetOrderId(string merchantName, string merchantOrderNo)
    {
        Guid.TryParse(merchantName + merchantOrderNo, out Guid orderNo);
        return orderNo;
    }
}
