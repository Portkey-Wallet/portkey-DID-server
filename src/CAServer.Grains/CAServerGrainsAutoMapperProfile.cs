using AElf.Types;
using AutoMapper;
using CAServer.Bookmark.Dtos;
using CAServer.Contacts;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Bookmark.Dtos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.ImTransfer;
using CAServer.Grains.Grain.Notify;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.Upgrade;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Grains.State;
using CAServer.Grains.State.Bookmark;
using CAServer.Grains.State.Chain;
using CAServer.Grains.State.Contacts;
using CAServer.Grains.State.CrossChain;
using CAServer.Grains.State.ImTransfer;
using CAServer.Grains.State.Notify;
using CAServer.Grains.State.Order;
using CAServer.Grains.State.PrivacyPermission;
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
using Portkey.Contracts.CA;

namespace CAServer.Grains;

public class CAServerGrainsAutoMapperProfile : Profile
{
    public CAServerGrainsAutoMapperProfile()
    {
        CreateMap<RegisterInfo, RegisterGrainDto>().ReverseMap();
        CreateMap<RecoveryInfo, RecoveryGrainDto>().ReverseMap();
        CreateMap<CAHolderState, CAHolderGrainDto>().ReverseMap();
        CreateMap<ChainState, ChainGrainDto>().ReverseMap();
        CreateMap<ContactAddress, ContactAddressDto>().ReverseMap();
        CreateMap<ContactState, ContactGrainDto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => new DateTimeOffset(s.ModificationTime).ToUnixTimeMilliseconds()));
        CreateMap<UserTokenState, UserTokenGrainDto>();
        CreateMap<CrossChainTransfer, CrossChainTransferDto>();
        CreateMap<CrossChainTransferDto, CrossChainTransfer>();
        CreateMap<GuardianState, GuardianGrainDto>();

        CreateMap<CreateHolderDto, CreateCAHolderInput>()
            .ForMember(d => d.DelegateInfo, opt => opt.MapFrom(e => e.ProjectDelegateInfo))
            .ForMember(d => d.GuardianApproved, opt => opt.MapFrom(e => new GuardianInfo
            {
                Type = e.GuardianInfo.Type,
                IdentifierHash = e.GuardianInfo.IdentifierHash,
                VerificationInfo = new VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id,
                    Signature = e.GuardianInfo.VerificationInfo.Signature,
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData
            }));

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
                }).ToList()))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData
            }))
            .ForMember(d => d.LoginGuardianIdentifierHash,
                opt => opt.MapFrom(g => g.LoginGuardianIdentifierHash));

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
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => long.Parse(src.TotalAmount)))
            .ForMember(dest => dest.GrabbedAmount,
                opt => opt.MapFrom(src => long.Parse(src.GrabbedAmount)))
            .ForMember(dest => dest.MinAmount,
                opt => opt.MapFrom(src => long.Parse(src.MinAmount)));
        CreateMap<GrabItem, GrabItemDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount.ToString()))
            .ReverseMap()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => long.Parse(src.Amount)));

        CreateMap<TransferGrainDto, ImTransferState>().ReverseMap();
        CreateMap<NftOrderGrainDto, NftOrderState>().ReverseMap();
        CreateMap<OrderSettlementState, OrderSettlementGrainDto>().ReverseMap();
        CreateMap<TransakAccessTokenDto, TransakAccessTokenState>();
        CreateMap<UpgradeState, UpgradeGrainDto>().ReverseMap();
    }
}