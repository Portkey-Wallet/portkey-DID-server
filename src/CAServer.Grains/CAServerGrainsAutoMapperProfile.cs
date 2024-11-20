using AElf;
using AElf.Types;
using AutoMapper;
using CAServer.Bookmark.Dtos;
using CAServer.Contacts;
using CAServer.EnumType;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.AddressBook;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Bookmark.Dtos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.Growth;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.ImTransfer;
using CAServer.Grains.Grain.Market;
using CAServer.Grains.Grain.Notify;
using CAServer.Grains.Grain.RedDot;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.Upgrade;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Grains.State;
using CAServer.Grains.State.AddressBook;
using CAServer.Grains.State.Bookmark;
using CAServer.Grains.State.Chain;
using CAServer.Grains.State.Contacts;
using CAServer.Grains.State.CrossChain;
using CAServer.Grains.State.Growth;
using CAServer.Grains.State.ImTransfer;
using CAServer.Grains.State.Market;
using CAServer.Grains.State.Notify;
using CAServer.Grains.State.Order;
using CAServer.Grains.State.PrivacyPermission;
using CAServer.Grains.State.RedDot;
using CAServer.Grains.State.ThirdPart;
using CAServer.Grains.State.RedPackage;
using CAServer.Grains.State.Tokens;
using CAServer.Grains.State.Upgrade;
using CAServer.Grains.State.UserExtraInfo;
using CAServer.PrivacyPermission.Dtos;
using CAServer.RedPackage.Dtos;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Portkey.Contracts.CA;

namespace CAServer.Grains;

public class CAServerGrainsAutoMapperProfile : Profile
{
    public CAServerGrainsAutoMapperProfile()
    {
        CreateMap<RegisterInfo, RegisterGrainDto>().ReverseMap();
        CreateMap<RecoveryInfo, RecoveryGrainDto>().ReverseMap();
        CreateMap<CAHolderState, CAHolderGrainDto>().ReverseMap();
        CreateMap<UserMarketTokenFavoritesState, UserMarketTokenFavoritesGrainDto>().ReverseMap();
        CreateMap<CryptoGiftState, CryptoGiftDto>();
        CreateMap<CryptoGiftDto, CryptoGiftState>();
        CreateMap<ChainState, ChainGrainDto>().ReverseMap();
        CreateMap<ContactAddress, ContactAddressDto>().ReverseMap();
        CreateMap<ContactState, ContactGrainDto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => new DateTimeOffset(s.ModificationTime).ToUnixTimeMilliseconds()));
        CreateMap<UserTokenState, UserTokenGrainDto>();
        CreateMap<CrossChainTransfer, CrossChainTransferDto>();
        CreateMap<CrossChainTransferDto, CrossChainTransfer>();
        CreateMap<GuardianState, GuardianGrainDto>();

        CreateMap<CreateHolderDto, CreateCAHolderInput>()
            .ForMember(d => d.DelegateInfo, opt => opt.MapFrom(e => new DelegateInfo()
            {
                ChainId = e.ProjectDelegateInfo.ChainId,
                ProjectHash = Hash.LoadFromHex(e.ProjectDelegateInfo.ProjectHash),
                IdentifierHash = Hash.LoadFromHex(e.ProjectDelegateInfo.IdentifierHash),
                ExpirationTime = e.ProjectDelegateInfo.ExpirationTime,
                Delegations =
                {
                    e.ProjectDelegateInfo.Delegations
                },
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(e.ProjectDelegateInfo.TimeStamp).ToTimestamp(),
                IsUnlimitedDelegate = e.ProjectDelegateInfo.IsUnlimitedDelegate,
                Signature = e.ProjectDelegateInfo.Signature
            }))
            .ForMember(d => d.GuardianApproved, opt => opt.MapFrom(e => new GuardianInfo
            {
                Type = e.GuardianInfo.Type,
                IdentifierHash = e.GuardianInfo.IdentifierHash,
                VerificationInfo = new VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id,
                    Signature = e.GuardianInfo.VerificationInfo.Signature,
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = e.GuardianInfo.ZkLoginInfo == null ? new ZkLoginInfo() : new ZkLoginInfo
                {
                    IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash,
                    Salt = e.GuardianInfo.ZkLoginInfo.Salt,
                    Nonce = e.GuardianInfo.ZkLoginInfo.Nonce,
                    ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof,
                    ZkProofInfo = e.GuardianInfo.ZkLoginInfo.ZkProofInfo,
                    Issuer = e.GuardianInfo.ZkLoginInfo.Issuer,
                    Kid = e.GuardianInfo.ZkLoginInfo.Kid,
                    CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId,
                    PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                    IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                        ? IdentifierHashType.Sha256Hash : IdentifierHashType.PoseidonHash,
                    NoncePayload = e.GuardianInfo.ZkLoginInfo.NoncePayload
                }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData
            }))
            .ForMember(t => t.ReferralCode,
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ReferralCode))
            .ForMember(t => t.ProjectCode,
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ProjectCode));
        
        CreateMap<CreateHolderDto, ReportPreCrossChainSyncHolderInfoInput>()
            .ForMember(d => d.GuardianApproved, opt => opt.MapFrom(e => new GuardianInfo
            {
                Type = e.GuardianInfo.Type,
                IdentifierHash = e.GuardianInfo.IdentifierHash,
                VerificationInfo = new VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id,
                    Signature = e.GuardianInfo.VerificationInfo.Signature,
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = e.GuardianInfo.ZkLoginInfo == null ? new ZkLoginInfo() : new ZkLoginInfo
                {
                    IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash,
                    Salt = e.GuardianInfo.ZkLoginInfo.Salt,
                    Nonce = e.GuardianInfo.ZkLoginInfo.Nonce,
                    ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof,
                    ZkProofInfo = e.GuardianInfo.ZkLoginInfo.ZkProofInfo,
                    Issuer = e.GuardianInfo.ZkLoginInfo.Issuer,
                    Kid = e.GuardianInfo.ZkLoginInfo.Kid,
                    CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId,
                    PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                    IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                        ? IdentifierHashType.Sha256Hash : IdentifierHashType.PoseidonHash,
                    NoncePayload = e.GuardianInfo.ZkLoginInfo.NoncePayload
                }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData
            }))
            .ForMember(d => d.CreateChainId, opt => opt.MapFrom(e => ChainHelper.ConvertBase58ToChainId(e.ChainId)));

        CreateMap<SocialRecoveryDto, SocialRecoveryInput>()
            .ForMember(d => d.GuardiansApproved,
                opt => opt.MapFrom(e => e.GuardianApproved.Select(g => new GuardianInfo
                {
                    Type = g.Type,
                    IdentifierHash = g.IdentifierHash,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = g.VerificationInfo.Id,
                        Signature = g.VerificationInfo.Signature,
                        VerificationDoc = g.VerificationInfo.VerificationDoc
                    }
                    // ZkLoginInfo = g.ZkLoginInfo == null ? new ZkLoginInfo() : new ZkLoginInfo
                    // {
                    //     IdentifierHash = g.ZkLoginInfo.IdentifierHash,
                    //     Salt = g.ZkLoginInfo.Salt,
                    //     Nonce = g.ZkLoginInfo.Nonce,
                    //     ZkProof = g.ZkLoginInfo.ZkProof,
                    //     ZkProofInfo = g.ZkLoginInfo.ZkProofInfo,
                    //     Issuer = g.ZkLoginInfo.Issuer,
                    //     Kid = g.ZkLoginInfo.Kid,
                    //     CircuitId = g.ZkLoginInfo.CircuitId,
                    //     PoseidonIdentifierHash = g.ZkLoginInfo.PoseidonIdentifierHash,
                    //     IdentifierHashType = g.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                    //         ? IdentifierHashType.Sha256Hash : IdentifierHashType.PoseidonHash,
                    //     NoncePayload = g.ZkLoginInfo.NoncePayload
                    // }
                }).ToList()))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData
            }))
            .ForMember(d => d.LoginGuardianIdentifierHash,
                opt => opt.MapFrom(g => g.LoginGuardianIdentifierHash))
            .ForMember(t => t.ReferralCode,
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ReferralCode))
            .ForMember(t => t.ProjectCode,
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ProjectCode));

        CreateMap<GetHolderInfoOutput, ValidateCAHolderInfoWithManagerInfosExistsInput>()
            .ForMember(d => d.LoginGuardians,
                opt => opt.MapFrom(e => new RepeatedField<Hash>
                    { e.GuardianList.Guardians.Where(g => g.IsLoginGuardian).Select(g => g.IdentifierHash).ToList() }))
            .ForMember(d => d.ManagerInfos, opt => opt.MapFrom(g => g.ManagerInfos))
            .ForMember(d => d.CaHash,
                opt => opt.MapFrom(g => g.CaHash))
            .ForMember(d => d.CreateChainId,
                opt => opt.MapFrom(g => g.CreateChainId));

        CreateMap<UserExtraInfoGrainDto, UserExtraInfoState>().ReverseMap();
        CreateMap<OrderState, OrderGrainDto>();
        CreateMap<OrderGrainDto, OrderCreatedDto>();
        CreateMap<OrderGrainDto, OrderState>();
        CreateMap<NotifyGrainDto, NotifyState>().ReverseMap();
        CreateMap<NotifyGrainDto, NotifyRulesGrainDto>();
        CreateMap<NotifyRulesGrainDto, NotifyRulesState>().ReverseMap();
        CreateMap<NotifyState, NotifyGrainResultDto>().ForMember(d => d.Id, opt => opt.MapFrom(e => e.RulesId));
        CreateMap<OrderStatusInfoGrainDto, OrderStatusInfoState>();
        CreateMap<OrderStatusInfoState, OrderStatusInfoGrainResultDto>();
        CreateMap<BookmarkItem, BookmarkGrainResultDto>();
        CreateMap<BookmarkItem, BookmarkResultDto>();

        CreateMap<PrivacyPermissionState, PrivacyPermissionDto>().ReverseMap();
        CreateMap<RedPackageState, SendRedPackageInputDto>()
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount.ToString()))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount)).ReverseMap();
        CreateMap<CryptoGiftState, SendRedPackageInputDto>()
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount.ToString()))
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount)).ReverseMap();
        CreateMap<RedPackageState, RedPackageDetailDto>()
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount.ToString()))
            .ForMember(dest => dest.GrabbedAmount,
                opt => opt.MapFrom(src => src.GrabbedAmount.ToString()))
            .ForMember(dest => dest.MinAmount,
                opt => opt.MapFrom(src => src.MinAmount.ToString()))
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.BucketNotClaimed, opt => opt.Ignore())
            .ForMember(dest => dest.BucketClaimed, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => long.Parse(src.TotalAmount)))
            .ForMember(dest => dest.GrabbedAmount,
                opt => opt.MapFrom(src => long.Parse(src.GrabbedAmount)))
            .ForMember(dest => dest.MinAmount,
                opt => opt.MapFrom(src => long.Parse(src.MinAmount)));
        CreateMap<GrabItem, GrabItemDto>()
            .ForMember(dest => dest.DisplayType, opt => opt.MapFrom(m => CryptoGiftDisplayType.Common))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.ToString()))
            .ReverseMap()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => long.Parse(src.Amount)));
        CreateMap<BucketItemDto, PreGrabBucketItemDto>();
        CreateMap<BucketItem, BucketItemDto>();
        CreateMap<TransferGrainDto, ImTransferState>().ReverseMap();
        CreateMap<NftOrderGrainDto, NftOrderState>().ReverseMap();
        CreateMap<OrderSettlementState, OrderSettlementGrainDto>().ReverseMap();
        CreateMap<TransakAccessTokenDto, TransakAccessTokenState>().ReverseMap();
        CreateMap<TreasuryOrderState, TreasuryOrderDto>().ReverseMap();
        CreateMap<PendingTreasuryOrderState, PendingTreasuryOrderDto>().ReverseMap();
        CreateMap<RedDotState, RedDotGrainDto>().ReverseMap();
        CreateMap<GrowthState, GrowthGrainDto>().ReverseMap();
        CreateMap<InviteInfo, GrowthGrainDto>().ReverseMap();
        CreateMap<UpgradeState, UpgradeGrainDto>().ReverseMap();
        CreateMap<AddressBookGrainDto, AddressBookState>().ReverseMap();
    }
}