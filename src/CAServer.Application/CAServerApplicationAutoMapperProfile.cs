using System;
using AutoMapper;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
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
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Hubs;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Etos;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using ContactAddress = CAServer.Grains.Grain.Contacts.ContactAddress;
using Token = CAServer.UserAssets.Dtos.Token;

namespace CAServer;

public class CAServerApplicationAutoMapperProfile : Profile
{
    public CAServerApplicationAutoMapperProfile()
    {
        CreateMap<UserTokenGrainDto, UserTokenEto>();
        CreateMap<UserTokenGrainDto, UserTokenDto>();
        CreateMap<UserTokenItem, UserTokenGrainDto>()
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(u => u.Token.ChainId))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Token.Decimals))
            .ForPath(t => t.Token.Address, m => m.MapFrom(u => u.Token.Address));
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
        CreateMap<IndexerTransactionFee, TransactionFee>();

        CreateMap<IndexerTokenInfo, Token>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.TokenInfo == null ? null : f.TokenInfo.Symbol))
            .ForMember(t => t.Decimals, m => m.MapFrom(f => f.TokenInfo == null ? new decimal() : f.TokenInfo.Decimals))
            .ForMember(t => t.TokenContractAddress,
                m => m.MapFrom(f =>
                    f.TokenInfo == null || f.TokenInfo.TokenContractAddress.IsNullOrEmpty()
                        ? null
                        : f.TokenInfo.TokenContractAddress));
        CreateMap<IndexerNftCollectionInfo, NftCollection>()
            .ForMember(t => t.ItemCount, m => m.MapFrom(f => f.TokenIds == null ? 0 : f.TokenIds.Count))
            .ForMember(t => t.ImageUrl,
                m => m.MapFrom(f =>
                    f.NftCollectionInfo == null ? null : f.NftCollectionInfo.ImageUrl))
            .ForMember(t => t.CollectionName,
                m => m.MapFrom(f => f.NftCollectionInfo == null ? null : f.NftCollectionInfo.TokenName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.NftCollectionInfo.Symbol));

        CreateMap<IndexerNftInfo, NftItem>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.Symbol))
            .ForMember(t => t.Alias, m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.TokenName))
            .ForMember(t => t.TokenContractAddress,
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.TokenContractAddress))
            .ForMember(t => t.ImageUrl,
                m => m.MapFrom(f =>
                    f.NftInfo == null ? null : f.NftInfo.ImageUrl));

        CreateMap<CAHolderTransactionAddress, RecentTransactionUser>()
            .ForMember(t => t.TransactionTime, m => m.MapFrom(f => f.TransactionTime.ToString()));

        CreateMap<IndexerSearchTokenNft, UserAsset>()
            .ForMember(t => t.Address, m => m.MapFrom(f => f.CaAddress))
            .ForMember(t => t.TokenInfo, m => m.MapFrom(f => f.TokenInfo == null ? null : new TokenInfoDto()))
            .ForMember(t => t.NftInfo, m => m.MapFrom(f => f.NftInfo == null ? null : new NftInfoDto()))
            .ForMember(t => t.Symbol,
                m => m.MapFrom(f =>
                    f.TokenInfo == null ? f.NftInfo == null ? null : f.NftInfo.Symbol : f.TokenInfo.Symbol));
        CreateMap<IndexerSearchTokenNft, TokenInfoDto>()
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.Balance.ToString()))
            .ForMember(t => t.Decimals,
                m => m.MapFrom(f => f.TokenInfo == null ? new decimal() : f.TokenInfo.Decimals))
            .ForMember(t => t.TokenContractAddress,
                m => m.MapFrom(f => f.TokenInfo == null ? null : f.TokenInfo.TokenContractAddress));

        CreateMap<IndexerSearchTokenNft, NftInfoDto>()
            .ForMember(t => t.ImageUrl,
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.ImageUrl))
            .ForMember(t => t.Alias, m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.TokenName))
            .ForMember(t => t.CollectionName,
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.CollectionName))
            .ForMember(t => t.Balance, m => m.MapFrom(f => f.NftInfo == null ? null : f.Balance.ToString()))
            .ForMember(t => t.TokenContractAddress,
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.TokenContractAddress));

        // user activity
        CreateMap<IndexerTransaction, GetActivityDto>()
            .ForMember(t => t.TransactionType, m => m.MapFrom(f => f.MethodName))
            .ForMember(t => t.NftInfo, m => m.MapFrom(f => f.NftInfo == null ? null : new NftDetail()))
            .ForMember(t => t.Symbol,
                m => m.MapFrom(f =>
                    f.TokenInfo == null ? (f.NftInfo == null ? null : f.NftInfo.Symbol) : f.TokenInfo.Symbol))
            .ForMember(t => t.Decimals,
                m => m.MapFrom(f =>
                    f.TokenInfo == null
                        ? (f.NftInfo == null ? null : f.NftInfo.Decimals.ToString())
                        : f.TokenInfo.Decimals.ToString()))
            .ForMember(t => t.Timestamp, m => m.MapFrom(f => f.Timestamp.ToString()))
            .ForMember(t => t.FromAddress,
                m => m.MapFrom(f =>
                    f.TransferInfo == null
                        ? f.FromAddress
                        : f.TransferInfo.FromCAAddress ?? f.TransferInfo.FromAddress))
            .ForMember(t => t.ToAddress, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToAddress))
            .ForMember(t => t.Amount,
                m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.Amount.ToString()))
            .ForMember(t => t.FromChainId,
                m => m.MapFrom(f => f.ChainId))
            .ForMember(t => t.ToChainId, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToChainId));

        CreateMap<VerifierServerInput, SendVerificationRequestInput>();
        CreateMap<SendVerificationRequestInput, VerifierCodeRequestDto>();
    }
}