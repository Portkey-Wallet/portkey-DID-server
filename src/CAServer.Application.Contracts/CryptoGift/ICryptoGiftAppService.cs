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

    public Task<CryptoGiftIdentityCodeDto> PreGrabCryptoGift(Guid redPackageId);

    public Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, int index, int amountDecimal, string ipAddress, string identityCode);

    public Task CheckClaimQuotaAfterLoginCondition(RedPackageDetailDto redPackageDetailDto, string caHash);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(string caHash, Guid redPackageId);

    public Task CryptoGiftTransferToRedPackage(Guid userId, string caAddress, ReferralInfo referralInfo, bool isNewUser, string ipAddress);

    public Task<CryptoGiftAppDto> GetCryptoGiftDetailFromGrainAsync(Guid redPackageId);

    public Task TestCryptoGiftTransferToRedPackage(string caHash, string caAddress,
        Guid id, string identityCode, bool isNewUser);

    public (string, string) GetIpAddressAndIdentity(Guid redPackageId);
}