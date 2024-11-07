using System;
using AutoMapper;
using CAServer.AddressBook.Dtos;
using CAServer.AddressBook.Etos;
using CAServer.Chain;
using CAServer.Guardian;
using CAServer.ContractEventHandler;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Etos.Chain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Contacts;
using CAServer.Growth.Etos;
using CAServer.IpInfo;
using CAServer.Notify.Etos;
using CAServer.RedDot.Etos;
using CAServer.Security.Etos;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.Tokens.Etos;
using CAServer.TwitterAuth.Etos;
using CAServer.Verifier.Etos;
using ContactAddress = CAServer.Entities.Es.ContactAddress;

namespace CAServer.EntityEventHandler.Core;

public class CAServerEventHandlerAutoMapperProfile : Profile
{
    public CAServerEventHandlerAutoMapperProfile()
    {
        CreateMap<CAServer.Entities.Es.ImInfo, CAServer.Contacts.ImInfo>().ReverseMap();
        CreateMap<ContactIndex, ContactCreateEto>();
        CreateMap<ContactCreateEto, ContactIndex>()
            .ForMember(t => t.Name, f => f.MapFrom(m => m.Name ?? string.Empty));
        CreateMap<ContactUpdateEto, ContactIndex>()
            .ForMember(t => t.Name, f => f.MapFrom(m => m.Name ?? string.Empty));
        CreateMap<ContactIndex, ContactUpdateEto>();
        CreateMap<ContactAddress, ContactAddressEto>().ReverseMap();
        CreateMap<CreateHolderEto, CreateHolderResultGrainDto>();
        CreateMap<RegisterGrainDto, AccountRegisterIndex>();
        CreateMap<AccountRegisterCreateEto, AccountRegisterIndex>();
        CreateMap<AccountRecoverCreateEto, AccountRecoverIndex>();
        CreateMap<AccelerateCreateHolderEto, AccelerateRegisterIndex>()
            .ForMember(t => t.Id, m => m.MapFrom(u => $"{u.Id.ToString()}_{u.ChainId}"))
            .ForMember(t => t.SessionId, m => m.MapFrom(u => u.Id));
        CreateMap<AccelerateSocialRecoveryEto, AccelerateRecoverIndex>()
            .ForMember(t => t.Id, m => m.MapFrom(u => $"{u.Id.ToString()}_{u.ChainId}"))
            .ForMember(t => t.SessionId, m => m.MapFrom(u => u.Id));
        CreateMap<SocialRecoveryEto, SocialRecoveryResultGrainDto>();
        CreateMap<RecoveryGrainDto, AccountRecoverIndex>();
        CreateMap<UserTokenEto, UserTokenIndex>()
            .ForPath(t => t.Token.Id, m => m.MapFrom(u => u.Token.Id))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(u => u.Token.ChainId))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Token.Decimals))
            .ForPath(t => t.Token.Address, m => m.MapFrom(u => u.Token.Address));

        CreateMap<UserTokenDeleteEto, UserTokenIndex>()
            .ForPath(t => t.Token.Id, m => m.MapFrom(u => u.Token.Id))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(u => u.Token.ChainId))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Token.Decimals))
            .ForPath(t => t.Token.Address, m => m.MapFrom(u => u.Token.Address));
        CreateMap<CAHolderGrainDto, CAHolderIndex>();
        CreateMap<UpdateCAHolderEto, CAHolderIndex>();
        CreateMap<DefaultToken, DefaultTokenInfo>();
        CreateMap<ChainCreateEto, ChainsInfoIndex>();
        CreateMap<ChainUpdateEto, ChainsInfoIndex>();
        CreateMap<ChainDeleteEto, ChainsInfoIndex>();
        CreateMap<ChainResultDto, ChainsInfoIndex>();
        CreateMap<ChainsInfoIndex, ChainResultDto>();
        CreateMap<GuardianEto, GuardianIndex>();
        CreateMap<OrderEto, RampOrderIndex>();
        CreateMap<UserExtraInfoEto, UserExtraInfoIndex>();
        CreateMap<NotifyEto, NotifyRulesIndex>();
        CreateMap<DeleteNotifyEto, NotifyRulesIndex>();
        CreateMap<CAServer.ThirdPart.Dtos.OrderStatusInfo, CAServer.Entities.Es.OrderStatusInfo>();
        CreateMap<CAServer.Contacts.CaHolderInfo, CAServer.Entities.Es.CaHolderInfo>().ReverseMap();
        CreateMap<OrderStatusInfoEto, OrderStatusInfoIndex>();
        CreateMap<UserTransferLimitHistoryEto, UserTransferLimitHistoryIndex>();
        CreateMap<DeleteCAHolderEto, CAHolderIndex>();
        CreateMap<GuardianDeleteEto, GuardianIndex>();
        CreateMap<OrderSettlementGrainDto, OrderSettlementIndex>().ReverseMap();
        CreateMap<CreateGrowthEto, GrowthIndex>();
        CreateMap<RedDot.Dtos.RedDotInfo, Entities.Es.RedDotInfo>().ReverseMap();
        CreateMap<RedDotEto, RedDotIndex>();
        CreateMap<TreasuryOrderDto, TreasuryOrderIndex>().ReverseMap();
        CreateMap<TwitterStatisticEto, TwitterStatisticIndex>();
        CreateMap<AccountReportEto, AccountReportIndex>()
            .ForMember(t => t.Id, m => m.MapFrom(f => f.CaHash))
            .ForMember(t => t.ClientType, m => m.MapFrom(f => f.ClientType.ToString()))
            .ForMember(t => t.OperationType, m => m.MapFrom(f => f.OperationType.ToString()))
            .ForMember(t => t.CreateTime, f => f.MapFrom(f => DateTime.UtcNow));

        CreateMap<IpInfoDto, CountryInfo>();
        CreateMap<AddressBookEto, AddressBookIndex>();
        CreateMap<CAServer.AddressBook.Dtos.ContactCaHolderInfo, CAServer.Entities.Es.ContactCaHolderInfo>();
        CreateMap<ContactAddressInfoDto, AddressInfo>();
    }
}