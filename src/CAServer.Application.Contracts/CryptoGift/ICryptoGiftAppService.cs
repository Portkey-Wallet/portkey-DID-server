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

    public Task<CryptoGiftIdentityCodeDto> PreGrabCryptoGift(Guid redPackageId, string random);

    public Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, int index, int amountDecimal, string ipAddress, string identityCode);

    public Task CheckClaimQuotaAfterLoginCondition(RedPackageDetailDto redPackageDetailDto, string caHash);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId, string random);

    public Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(string caHash, Guid redPackageId, string random);

    public Task CryptoGiftTransferToRedPackage(Guid userId, string caHash, string caAddress, ReferralInfo referralInfo, bool isNewUser, string ipAddress);

    public Task<CryptoGiftAppDto> GetCryptoGiftDetailFromGrainAsync(Guid redPackageId);

    public (string, string) GetIpAddressAndIdentity(Guid redPackageId, string random);

    public Task<List<CryptoGiftSentNumberDto>> ComputeCryptoGiftNumber(bool newUsersOnly, string[] symbols, long createTime);

    public Task<List<CryptoGiftClaimDto>> ComputeCryptoGiftClaimStatistics(bool newUsersOnly, string[] symbols, long createTime);
}