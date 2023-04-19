using AElf.Types;
using AutoMapper;
using CAServer.Contacts;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.Notify;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Grains.State;
using CAServer.Grains.State.Chain;
using CAServer.Grains.State.Contacts;
using CAServer.Grains.State.CrossChain;
using CAServer.Grains.State.Notify;
using CAServer.Grains.State.Order;
using CAServer.Grains.State.Tokens;
using CAServer.Grains.State.UserExtraInfo;
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
                opt => opt.MapFrom(g => g.CaHash));

        CreateMap<UserExtraInfoGrainDto, UserExtraInfoState>().ReverseMap();
        CreateMap<OrderState, OrderGrainDto>();
        CreateMap<OrderGrainDto, OrderCreatedDto>();
        CreateMap<OrderGrainDto, OrderState>();
        CreateMap<NotifyGrainDto, NotifyState>().ReverseMap();
        CreateMap<NotifyGrainDto, NotifyRulesGrainDto>();
        CreateMap<NotifyRulesGrainDto, NotifyRulesState>().ReverseMap();
        CreateMap<NotifyState, NotifyGrainResultDto>().ForMember(d => d.Id, opt => opt.MapFrom(e => e.RulesId));
    }
}