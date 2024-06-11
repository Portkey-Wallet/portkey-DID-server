using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.CryptoGift.Dtos;
using CAServer.RedPackage.Dtos;

namespace CAServer.CryptoGift;

public interface ICryptoGiftAppService
{
    public Task<CryptoGiftHistoryItemDto> GetFirstCryptoGiftHistoryDetailAsync(Guid senderId);
    public Task<List<CryptoGiftHistoryItemDto>> ListCryptoGiftHistoriesAsync(Guid senderId);

    public Task<PreGrabbedDto> ListCryptoPreGiftGrabbedItems(Guid redPackageId);

    public Task<string> PreGrabCryptoGift(Guid redPackageId);

    public Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, int index, int amountDecimal);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(Guid receiverId, Guid redPackageId);

    public Task CryptoGiftTransferToRedPackage(string caHash, string caAddress, ReferralInfo referralInfo, bool isNewUser);
}