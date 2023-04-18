using System;
using AutoMapper;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Provider;
using CAServer.Chain;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.ContractEventHandler;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Etos.Chain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Contacts;
using CAServer.Hubs;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using ContactAddress = CAServer.Grains.Grain.Contacts.ContactAddress;
using NftInfo = CAServer.UserAssets.Dtos.NftInfo;
using NftProtocol = CAServer.UserAssets.Provider.NftProtocol;
using Token = CAServer.Tokens.Token;
using TokenInfo = CAServer.UserAssets.Dtos.TokenInfo;

namespace CAServer;

public class CAServerApplicationAutoMapperProfile : Profile
{
    public CAServerApplicationAutoMapperProfile()
    {
        CreateMap<UserTokenItem, Token>();
        // Contact
        CreateMap<ContactAddressDto, ContactAddress>().ReverseMap();
        CreateMap<ContactAddressDto, ContactAddressEto>();
        CreateMap<CreateUpdateContactDto, ContactGrainDto>();
        CreateMap<ContactGrainDto, ContactResultDto>();
        CreateMap<ContactDto, ContactCreateEto>().ForMember(c => c.ModificationTime,
                d => d.MapFrom(s => TimeHelper.GetDateTimeFromTimeStamp(s.ModificationTime)))
            .ForMember(c => c.Id, d => d.Condition(src => src.Id != Guid.Empty));

        CreateMap<ContactDto, ContactUpdateEto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => TimeHelper.GetDateTimeFromTimeStamp(s.ModificationTime)));

        CreateMap<ContactIndex, ContactDto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => new DateTimeOffset(s.ModificationTime).ToUnixTimeMilliseconds()));
        CreateMap<Entities.Es.ContactAddress, ContactAddressDto>().ReverseMap();

        CreateMap<HubRequestContextDto, HubRequestContext>();
        CreateMap<RegisterDto, RegisterGrainDto>();
        CreateMap<CreateHolderEto, CreateHolderResultGrainDto>();
        CreateMap<RegisterGrainDto, AccountRegisterCreateEto>();
        CreateMap<RegisterDto, CAAccountEto>();
        CreateMap<RecoveryDto, RecoveryGrainDto>();
        CreateMap<RecoveryGrainDto, AccountRecoverCreateEto>();
        CreateMap<RecoveryDto, CAAccountRecoveryEto>();
        CreateMap<RegisterGrainDto, AccountRegisterCompletedEto>();
        CreateMap<RecoveryGrainDto, AccountRecoverCompletedEto>();
        CreateMap<SocialRecoveryEto, SocialRecoveryResultGrainDto>();
        CreateMap<CreateUserEto, CAHolderDto>();
        CreateMap<CreateUserEto, CAHolderGrainDto>();
        CreateMap<CAHolderGrainDto, CreateCAHolderEto>();
        CreateMap<CAHolderGrainDto, UpdateCAHolderEto>();
        CreateMap<CAHolderGrainDto, CAHolderResultDto>();
        CreateMap<CreateUpdateChainDto, ChainGrainDto>();
        CreateMap<ChainGrainDto, ChainResultDto>();
        CreateMap<ChainGrainDto, ChainCreateEto>();
        CreateMap<ChainGrainDto, ChainUpdateEto>();
        CreateMap<ChainGrainDto, ChainDeleteEto>();

        CreateMap<VerificationSignatureRequestDto, VierifierCodeRequestInput>();

        CreateMap<ChainDto, ChainUpdateEto>();
        // user assets
        CreateMap<TokenBalance, UserAssets.Dtos.Token>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.IndexerTokenInfo.Symbol))
            .ForMember(t => t.Decimal, m => m.MapFrom(f => f.IndexerTokenInfo.Decimals));
        CreateMap<NftProtocol, UserAssets.Dtos.NftProtocol>()
            .ForMember(t => t.ItemCount, m => m.MapFrom(f => f.TokenIds.Count))
            .ForMember(t => t.ImageUrl, m => m.MapFrom(f => f.NftProtocolInfo.ImageUrl))
            .ForMember(t => t.ProtocolName, m => m.MapFrom(f => f.NftProtocolInfo.ProtocolName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.NftProtocolInfo.Symbol))
            .ForMember(t => t.NftType, m => m.MapFrom(f => f.NftProtocolInfo.NftType));
        CreateMap<UserNftInfo, NftItem>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.NftInfo.Symbol))
            .ForMember(t => t.TokenId, m => m.MapFrom(f => f.NftInfo.TokenId))
            .ForMember(t => t.Alias, m => m.MapFrom(f => f.NftInfo.Alias))
            .ForMember(t => t.ImageUrl, m => m.MapFrom(f => f.NftInfo.ImageUrl));
        CreateMap<CAHolderTransactionAddress, RecentTransactionUser>()
            .ForMember(t => t.TransactionTime, m => m.MapFrom(f => f.TransactionTime.ToString()));
        CreateMap<IndexerUserAsset, UserAsset>()
            .ForMember(t => t.Symbol,
                m => m.MapFrom(f => f.IndexerTokenInfo != null ? f.IndexerTokenInfo.Symbol : f.NftInfo.Symbol))
            .ForMember(t => t.Address,
                m => m.MapFrom(f =>
                    f.IndexerTokenInfo != null
                        ? f.IndexerTokenInfo.TokenContractAddress
                        : f.NftInfo.NftContractAddress));
        CreateMap<IndexerUserAsset, TokenInfo>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Decimal, m => m.MapFrom(f => f.IndexerTokenInfo.Decimals.ToString()));
        CreateMap<IndexerUserAsset, NftInfo>()
            .ForMember(t => t.ImageUrl, m => m.MapFrom(f => f.NftInfo.Uri))
            .ForMember(t => t.Alias, m => m.MapFrom(f => f.NftInfo.Alias))
            .ForMember(t => t.TokenId, m => m.MapFrom(f => f.NftInfo.TokenId.ToString()))
            .ForMember(t => t.ProtocolName, m => m.MapFrom(f => f.NftInfo.ProtocolName))
            .ForMember(t => t.Quantity, m => m.MapFrom(f => f.NftInfo.Quantity.ToString()));
        // user activity
        CreateMap<IndexerTransaction, GetActivitiesDto>()
            .ForMember(t => t.TransactionType, m => m.MapFrom(f => f.MethodName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.TokenInfo != null ? f.TokenInfo.Symbol : ""))
            .ForMember(t => t.Decimal,
                m => m.MapFrom(f => f.TokenInfo == null ? null : f.TokenInfo.Decimals.ToString()))
            .ForMember(t => t.TimeStamp, m => m.MapFrom(f => f.Timestamp.ToString()))
            .ForMember(t => t.NftInfo,
                m => m.MapFrom(f =>
                    f.NftInfo == null
                        ? null
                        : new NftDetail()
                            { ImageUrl = f.NftInfo.Url, Alias = f.NftInfo.Alias, NftId = f.NftInfo.NftId.ToString() }))
            .ForMember(t => t.ToAddress, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToAddress))
            .ForMember(t => t.Amount,
                m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.Amount.ToString()))
            .ForMember(t => t.FromChainId,
                m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.FromChainId))
            .ForMember(t => t.ToChainId, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToChainId));

        CreateMap<VerifierServerInput, SendVerificationRequestInput>();
        CreateMap<SendVerificationRequestInput, VerifierCodeRequestDto>();
        

    }
}