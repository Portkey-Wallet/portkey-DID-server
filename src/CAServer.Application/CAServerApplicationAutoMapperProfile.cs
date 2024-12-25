using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Types;
using AutoMapper;
using CAServer.Awaken;
using CAServer.AddressBook.Dtos;
using CAServer.AddressBook.Etos;
using CAServer.Bookmark.Dtos;
using CAServer.Bookmark.Etos;
using CAServer.CAAccount.Dtos;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Chain;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.ContractEventHandler;
using CAServer.DataReporting.Dtos;
using CAServer.DataReporting.Etos;
using CAServer.Dtos;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Etos;
using CAServer.Etos.Chain;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.AddressBook;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Bookmark.Dtos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.FreeMint;
using CAServer.Grains.Grain.Growth;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.ImTransfer;
using CAServer.Grains.Grain.Notify;
using CAServer.Grains.Grain.RedDot;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.Upgrade;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Grains.State;
using CAServer.Grains.State.UserGuide;
using CAServer.Grains.State.ValidateOriginChainId;
using CAServer.Growth.Dtos;
using CAServer.Growth.Etos;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Hubs;
using CAServer.ImTransfer.Dtos;
using CAServer.ImTransfer.Etos;
using CAServer.ImUser.Dto;
using CAServer.IpInfo;
using CAServer.Market;
using CAServer.Message.Dtos;
using CAServer.Message.Etos;
using CAServer.Notify.Dtos;
using CAServer.Notify.Etos;
using CAServer.Options;
using CAServer.PrivacyPolicy.Dtos;
using CAServer.RedPackage.Dtos;
using CAServer.ThirdPart;
using CAServer.RedDot.Dtos;
using CAServer.RedDot.Etos;
using CAServer.Search.Dtos;
using CAServer.Tab.Dtos;
using CAServer.Tab.Etos;
using CAServer.Telegram.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Etos;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Etos;
using CAServer.Tokens.Provider;
using CAServer.Transfer.Dtos;
using CAServer.Upgrade.Dtos;
using CAServer.Upgrade.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using CAServer.UserGuide.Dtos;
using CAServer.ValidateOriginChainId.Dtos;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using CoinGecko.Entities.Response.Coins;
using Google.Protobuf;
using Portkey.Contracts.CA;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AutoMapper;
using ContactAddress = CAServer.Grains.Grain.Contacts.ContactAddress;
using GuardianInfo = CAServer.Account.GuardianInfo;
using GuardianType = CAServer.Account.GuardianType;
using ImInfo = CAServer.Contacts.ImInfo;
using RedDotInfo = CAServer.Entities.Es.RedDotInfo;
using Token = CAServer.UserAssets.Dtos.Token;
using VerificationInfo = CAServer.Account.VerificationInfo;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.IdentityModel.Tokens;
using Enum = System.Enum;
using GuardianDto = CAServer.Guardian.GuardianDto;
using ManagerInfoDto = CAServer.Guardian.ManagerInfoDto;
using NoncePayload = Portkey.Contracts.CA.NoncePayload;

namespace CAServer;

public class CAServerApplicationAutoMapperProfile : Profile
{
    public CAServerApplicationAutoMapperProfile()
    {
        CreateMap<GuardiansDto, GuardiansAppDto>();
        CreateMap<GuardianEto, GuardianIndex>();
        CreateMap<VerifiedZkLoginRequestDto, VerifyTokenRequestDto>();
        CreateMap<GoogleUserInfoDto, CAServer.Verifier.Dtos.UserExtraInfo>();
        CreateMap<UserTokenGrainDto, UserTokenEto>();
        CreateMap<UserTokenGrainDto, UserTokenDeleteEto>();
        CreateMap<UserTokenGrainDto, UserTokenDto>();
        CreateMap<UserTokenItem, UserTokenGrainDto>()
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(u => u.Token.Symbol))
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(u => u.Token.ChainId))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(u => u.Token.Decimals))
            .ForPath(t => t.Token.Address, m => m.MapFrom(u => u.Token.Address))
            .ForPath(t => t.Token.ImageUrl, m => m.MapFrom(u => u.Token.ImageUrl));
        // Contact
        CreateMap<ContactAddressDto, ContactAddress>().ReverseMap();
        CreateMap<ContactAddressDto, ContactAddressEto>();
        CreateMap<CreateUpdateContactDto, ContactGrainDto>();
        CreateMap<ContactIndex, ContactResultDto>();
        CreateMap<ContactGrainDto, ContactResultDto>()
            .ForMember(t => t.Name, f => f.MapFrom(m => m.Name ?? string.Empty));

        CreateMap<ContactDto, ContactCreateEto>().ForMember(c => c.ModificationTime,
                d => d.MapFrom(s => TimeHelper.GetDateTimeFromTimeStamp(s.ModificationTime)))
            .ForMember(c => c.Id, d => d.Condition(src => src.Id != Guid.Empty));

        CreateMap<ContactDto, ContactUpdateEto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => TimeHelper.GetDateTimeFromTimeStamp(s.ModificationTime)));

        CreateMap<ContactIndex, ContactDto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => new DateTimeOffset(s.ModificationTime).ToUnixTimeMilliseconds()));
        CreateMap<Entities.Es.ContactAddress, ContactAddressDto>().ReverseMap();
        CreateMap<TradePairsItemToken, CAServer.UserAssets.Dtos.Token>()
            .ForMember(d => d.Symbol, f => f.MapFrom(s => s.Symbol))
            .ForMember(d => d.Decimals, f => f.MapFrom(s => s.Decimals))
            .ForMember(d => d.ChainId, f => f.MapFrom(s => s.ChainId))
            .ForMember(d => d.ImageUrl, f => f.MapFrom(s => s.ImageUri))
            ;
        CreateMap<HubRequestContextDto, HubRequestContext>();
        CreateMap<RegisterDto, RegisterGrainDto>();
        CreateMap<CreateHolderEto, CreateHolderResultGrainDto>();
        CreateMap<RegisterGrainDto, AccountRegisterCreateEto>();
        CreateMap<RegisterDto, CAAccountEto>();
        CreateMap<RecoveryDto, RecoveryGrainDto>();
        CreateMap<ManagerInfo, ManagerDto>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(source => source.Address.ToBase58()))
            .ForMember(dest => dest.ExtraData, opt => opt.MapFrom(source => source.ExtraData));
        CreateMap<RecoveryGrainDto, AccountRecoverCreateEto>();
        CreateMap<GuardianInfo, Portkey.Contracts.CA.GuardianInfo>()
            .ForMember(dest => dest.IdentifierHash, opt => opt.MapFrom(src => Hash.LoadFromHex(src.IdentifierHash)))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (Portkey.Contracts.CA.GuardianType)(int)src.Type))
            .ForPath(dest => dest.VerificationInfo, opt => opt.MapFrom(src => new Portkey.Contracts.CA.VerificationInfo
            {
                Id = src.VerificationInfo.Id.IsNullOrWhiteSpace()
                    ? Hash.Empty : Hash.LoadFromHex(src.VerificationInfo.Id),
                Signature = src.VerificationInfo.Signature.IsNullOrWhiteSpace() 
                    ? ByteString.Empty : ByteStringHelper.FromHexString(src.VerificationInfo.Signature),
                VerificationDoc = src.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace() 
                    ? string.Empty : src.VerificationInfo.VerificationDoc
            }))
            .ForPath(dest => dest.ZkLoginInfo, opt => opt.MapFrom(src => new ZkLoginInfo
                {
                    IdentifierHash = src.ZkLoginInfo.IdentifierHash.IsNullOrWhiteSpace()
                        ? Hash.Empty : Hash.LoadFromHex(src.ZkLoginInfo.IdentifierHash),
                    Salt = src.ZkLoginInfo.Salt.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.Salt,
                    Nonce = src.ZkLoginInfo.Nonce.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.Nonce,
                    ZkProof = src.ZkLoginInfo.ZkProof.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.ZkProof,
                    Issuer = src.ZkLoginInfo.Issuer.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.Issuer,
                    Kid = src.ZkLoginInfo.Kid.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.Kid,
                    CircuitId = src.ZkLoginInfo.CircuitId.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.CircuitId,
                    PoseidonIdentifierHash = src.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty() ? string.Empty : src.ZkLoginInfo.PoseidonIdentifierHash,
                    IdentifierHashType = src.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                        ? IdentifierHashType.Sha256Hash : IdentifierHashType.PoseidonHash,
                    NoncePayload = new NoncePayload
                    {
                        AddManagerAddress = new AddManager
                        {
                            CaHash = src.ZkLoginInfo.NoncePayload.AddManager.CaHash.IsNullOrWhiteSpace()
                                ? Hash.Empty : Hash.LoadFromHex(src.ZkLoginInfo.NoncePayload.AddManager.CaHash),
                            ManagerAddress = src.ZkLoginInfo.NoncePayload.AddManager.ManagerAddress.IsNullOrWhiteSpace()
                                ? new Address() : Address.FromBase58(src.ZkLoginInfo.NoncePayload.AddManager.ManagerAddress),
                            Timestamp = new Timestamp
                            {
                                Seconds = src.ZkLoginInfo.NoncePayload.AddManager.Timestamp,
                                Nanos = 0
                            }
                        }
                    },
                    ZkProofInfo = new ZkProofInfo
                    {
                        ZkProofPiA = { src.ZkLoginInfo.ZkProofPiA },
                        ZkProofPiB1 = { src.ZkLoginInfo.ZkProofPiB1 },
                        ZkProofPiB2 = { src.ZkLoginInfo.ZkProofPiB2 },
                        ZkProofPiB3 = { src.ZkLoginInfo.ZkProofPiB3 },
                        ZkProofPiC = { src.ZkLoginInfo.ZkProofPiC }
                    }
                }));
        CreateMap<AccountRegisterCreateEto, CreateHolderDto>()
            .ForMember(d => d.Platform,
                opt => opt.MapFrom(e => Enum.IsDefined(typeof(Platform), (int)e.Source) ? (Platform)(int)e.Source : Platform.Undefined))
            .ForMember(d => d.GuardianInfo, opt => opt.MapFrom(e => new Portkey.Contracts.CA.GuardianInfo
            {
                Type = (Portkey.Contracts.CA.GuardianType)(int)e.GuardianInfo.Type,
                IdentifierHash = Hash.LoadFromHex(e.GuardianInfo.IdentifierHash),
                VerificationInfo = new Portkey.Contracts.CA.VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id.IsNullOrWhiteSpace()
                        ? Hash.Empty
                        : Hash.LoadFromHex(e.GuardianInfo.VerificationInfo.Id),
                    Signature = e.GuardianInfo.VerificationInfo.Signature.IsNullOrWhiteSpace()
                        ? ByteString.Empty
                        : ByteStringHelper.FromHexString(e.GuardianInfo.VerificationInfo.Signature),
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace()
                        ? string.Empty
                        : e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = e.GuardianInfo.ZkLoginInfo == null
                    ? new ZkLoginInfo()
                    : new ZkLoginInfo
                    {
                        IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash.IsNullOrWhiteSpace()
                            ? Hash.Empty
                            : Hash.LoadFromHex(e.GuardianInfo.ZkLoginInfo.IdentifierHash),
                        Salt = e.GuardianInfo.ZkLoginInfo.Salt.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Salt,
                        Nonce = e.GuardianInfo.ZkLoginInfo.Nonce.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Nonce,
                        ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.ZkProof,
                        Issuer = e.GuardianInfo.ZkLoginInfo.Issuer.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Issuer,
                        Kid = e.GuardianInfo.ZkLoginInfo.Kid.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Kid,
                        CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.CircuitId,
                        PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                        IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? IdentifierHashType.Sha256Hash
                            : IdentifierHashType.PoseidonHash,
                        NoncePayload = new NoncePayload
                        {
                            AddManagerAddress = new AddManager
                            {
                                CaHash = e.CaHash.IsNullOrWhiteSpace()
                                    ? Hash.Empty
                                    : Hash.LoadFromHex(e.CaHash),
                                ManagerAddress = e.ManagerInfo.Address.IsNullOrWhiteSpace()
                                    ? new Address()
                                    : Address.FromBase58(e.ManagerInfo.Address),
                                Timestamp = new Timestamp
                                {
                                    Seconds = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManager.Timestamp,
                                    Nanos = 0
                                }
                            }
                        },
                        ZkProofInfo = new ZkProofInfo
                        {
                            ZkProofPiA = { e.GuardianInfo.ZkLoginInfo.ZkProofPiA },
                            ZkProofPiB1 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB1 },
                            ZkProofPiB2 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB2 },
                            ZkProofPiB3 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB3 },
                            ZkProofPiC = { e.GuardianInfo.ZkLoginInfo.ZkProofPiC }
                        }
                    }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = Address.FromBase58(e.ManagerInfo.Address),
                ExtraData = e.ManagerInfo.ExtraData
            }));

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
        CreateMap<GoogleUserInfoDto, Verifier.Dtos.UserExtraInfo>();
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
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.TokenContractAddress))
            .ForMember(t => t.Decimals,
                m => m.MapFrom(f => f.NftInfo == null ? null : f.NftInfo.Decimals.ToString()));

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
                        : f.TransferInfo.FromCAAddress.IsNullOrWhiteSpace()
                            ? f.TransferInfo.FromAddress
                            : f.TransferInfo.FromCAAddress))
            .ForMember(t => t.ToAddress, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToAddress))
            .ForMember(t => t.Amount,
                m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.Amount.ToString()))
            .ForMember(t => t.FromChainId,
                m => m.MapFrom(f => f.ChainId))
            .ForMember(t => t.ToChainId, m => m.MapFrom(f => f.TransferInfo == null ? "" : f.TransferInfo.ToChainId));

        CreateMap<VerifierServerInput, SendVerificationRequestInput>();
        CreateMap<SendVerificationRequestInput, VerifierCodeRequestDto>();
        CreateMap<GuardianGrainDto, GuardianEto>();
        CreateMap<GuardianGrainDto, GuardianDeleteEto>();
        CreateMap<GuardianIndex, GuardianIndexDto>().ReverseMap();
        CreateMap<UserExtraInfoIndex, UserExtraInfoIndexDto>();

        CreateMap<ManagerInfo, ManagerInfoDto>()
            .ForMember(t => t.Address, m => m.MapFrom(f => f.Address.ToBase58()));
        CreateMap<Portkey.Contracts.CA.Guardian, GuardianDto>()
            .ForMember(t => t.IdentifierHash, m => m.MapFrom(f => f.IdentifierHash.ToHex()))
            .ForMember(t => t.VerifierId, m => m.MapFrom(f => f.VerifierId.ToHex()))
            .ForMember(t => t.Type, m => m.MapFrom(f => (GuardianIdentifierType)(int)f.Type));

        CreateMap<GuardianList, GuardianListDto>();

        CreateMap<GetHolderInfoOutput, GuardianResultDto>()
            .ForMember(t => t.CaHash, m => m.MapFrom(f => f.CaHash.ToHex()))
            .ForMember(t => t.CaAddress, m => m.MapFrom(f => f.CaAddress.ToBase58()))
            .ForMember(t => t.CreateChainId,
                m => m.MapFrom(f =>
                    f.CreateChainId > 0 ? ChainHelper.ConvertChainIdToBase58(f.CreateChainId) : string.Empty));
        // .ForPath(t => t.GuardianList, m => m.MapFrom(f => f.GuardianList.Guardians));

        //used by the ContractService class
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
            .ForMember(d => d.GuardianApproved, opt => opt.MapFrom(e => new Portkey.Contracts.CA.GuardianInfo
            {
                Type = e.GuardianInfo.Type,
                IdentifierHash = e.GuardianInfo.IdentifierHash,
                VerificationInfo = new Portkey.Contracts.CA.VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id,
                    Signature = e.GuardianInfo.VerificationInfo.Signature,
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = new Portkey.Contracts.CA.ZkLoginInfo()
                {
                    IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash,
                    Issuer = e.GuardianInfo.ZkLoginInfo.Issuer,
                    Kid = e.GuardianInfo.ZkLoginInfo.Kid,
                    Nonce = e.GuardianInfo.ZkLoginInfo.Nonce,
                    ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof,
                    Salt = e.GuardianInfo.ZkLoginInfo.Salt,
                    CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId,
                    PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                    IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                        ? IdentifierHashType.Sha256Hash
                        : IdentifierHashType.PoseidonHash,
                    NoncePayload = new NoncePayload()
                    {
                        AddManagerAddress = new AddManager()
                        {
                            CaHash = e.CaHash,
                            ManagerAddress = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManagerAddress.ManagerAddress,
                            Timestamp = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManagerAddress.Timestamp
                        }
                    },
                    ZkProofInfo = new ZkProofInfo
                    {
                        ZkProofPiA = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiA },
                        ZkProofPiB1 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB1 },
                        ZkProofPiB2 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB2 },
                        ZkProofPiB3 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB3 },
                        ZkProofPiC = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiC }
                    }
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
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ProjectCode))
            .ForMember(t => t.Platform, f => f.MapFrom(m => m.Platform));

        CreateMap<CreateHolderDto, ReportPreCrossChainSyncHolderInfoInput>()
            .ForMember(d => d.GuardianApproved, opt => opt.MapFrom(e => new Portkey.Contracts.CA.GuardianInfo
            {
                Type = e.GuardianInfo.Type,
                IdentifierHash = e.GuardianInfo.IdentifierHash,
                VerificationInfo = new Portkey.Contracts.CA.VerificationInfo
                {
                    Id = e.GuardianInfo.VerificationInfo.Id,
                    Signature = e.GuardianInfo.VerificationInfo.Signature,
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = new ZkLoginInfo()
                {
                    IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash,
                    Issuer = e.GuardianInfo.ZkLoginInfo.Issuer,
                    Kid = e.GuardianInfo.ZkLoginInfo.Kid,
                    Nonce = e.GuardianInfo.ZkLoginInfo.Nonce,
                    ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof,
                    Salt = e.GuardianInfo.ZkLoginInfo.Salt,
                    CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId,
                    PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                    IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                        ? IdentifierHashType.Sha256Hash
                        : IdentifierHashType.PoseidonHash,
                    NoncePayload = new NoncePayload()
                    {
                        AddManagerAddress = new AddManager()
                        {
                            CaHash = e.CaHash,
                            ManagerAddress = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManagerAddress.ManagerAddress,
                            Timestamp = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManagerAddress.Timestamp
                        }
                    },
                    ZkProofInfo = new ZkProofInfo
                    {
                        ZkProofPiA = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiA },
                        ZkProofPiB1 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB1 },
                        ZkProofPiB2 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB2 },
                        ZkProofPiB3 = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiB3 },
                        ZkProofPiC = { e.GuardianInfo.ZkLoginInfo.ZkProofInfo.ZkProofPiC }
                    }
                }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = e.ManagerInfo.Address,
                ExtraData = e.ManagerInfo.ExtraData,
                Platform = e.Platform
            }))
            .ForMember(d => d.CreateChainId, opt => opt.MapFrom(e => ChainHelper.ConvertBase58ToChainId(e.ChainId)));

        CreateMap<SocialRecoveryDto, SocialRecoveryInput>()
            .ForMember(d => d.GuardiansApproved,
                opt => opt.MapFrom(e => e.GuardianApproved.Select(g => new Portkey.Contracts.CA.GuardianInfo
                {
                    Type = g.Type,
                    IdentifierHash = g.IdentifierHash,
                    VerificationInfo = new Portkey.Contracts.CA.VerificationInfo
                    {
                        Id = g.VerificationInfo.Id,
                        Signature = g.VerificationInfo.Signature,
                        VerificationDoc = g.VerificationInfo.VerificationDoc
                    },
                    ZkLoginInfo = new ZkLoginInfo()
                    {
                        IdentifierHash = g.ZkLoginInfo == null ? Hash.Empty : g.ZkLoginInfo.IdentifierHash,
                        Issuer = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.Issuer,
                        Kid = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.Kid,
                        Nonce = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.Nonce,
                        ZkProof = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.ZkProof,
                        Salt = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.Salt,
                        CircuitId = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.CircuitId,
                        PoseidonIdentifierHash = g.ZkLoginInfo == null ? "" : g.ZkLoginInfo.PoseidonIdentifierHash,
                        IdentifierHashType = g.ZkLoginInfo == null
                            ? IdentifierHashType.Sha256Hash
                            : (g.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                                ? IdentifierHashType.Sha256Hash
                                : IdentifierHashType.PoseidonHash),
                        NoncePayload = new NoncePayload()
                        {
                            AddManagerAddress = new AddManager()
                            {
                                CaHash = e.CaHash.IsNullOrEmpty() ? Hash.Empty : e.CaHash,
                                ManagerAddress = g.ZkLoginInfo == null || g.ZkLoginInfo.NoncePayload == null
                                                                       || g.ZkLoginInfo.NoncePayload
                                                                           .AddManagerAddress == null
                                    ? new Address()
                                    : g.ZkLoginInfo.NoncePayload.AddManagerAddress.ManagerAddress,
                                Timestamp = g.ZkLoginInfo == null || g.ZkLoginInfo.NoncePayload == null
                                                                  || g.ZkLoginInfo.NoncePayload.AddManagerAddress ==
                                                                  null
                                    ? new Timestamp()
                                    : g.ZkLoginInfo.NoncePayload.AddManagerAddress.Timestamp
                            }
                        },
                        ZkProofInfo = g.ZkLoginInfo == null
                            ? new ZkProofInfo()
                            : new ZkProofInfo
                            {
                                ZkProofPiA = { g.ZkLoginInfo.ZkProofInfo.ZkProofPiA },
                                ZkProofPiB1 = { g.ZkLoginInfo.ZkProofInfo.ZkProofPiB1 },
                                ZkProofPiB2 = { g.ZkLoginInfo.ZkProofInfo.ZkProofPiB2 },
                                ZkProofPiB3 = { g.ZkLoginInfo.ZkProofInfo.ZkProofPiB3 },
                                ZkProofPiC = { g.ZkLoginInfo.ZkProofInfo.ZkProofPiC }
                            }
                    }
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
                f => f.MapFrom(m => m.ReferralInfo == null ? string.Empty : m.ReferralInfo.ProjectCode))
            .ForMember(t => t.Platform,
                f => f.MapFrom(m => m.Platform));

        CreateMap<GetHolderInfoOutput, ValidateCAHolderInfoWithManagerInfosExistsInput>()
            .ForMember(d => d.LoginGuardians,
                opt => opt.MapFrom(e => new RepeatedField<Hash>
                    { e.GuardianList.Guardians.Where(g => g.IsLoginGuardian).Select(g => g.IdentifierHash).ToList() }))
            .ForMember(d => d.ManagerInfos, opt => opt.MapFrom(g => g.ManagerInfos))
            .ForMember(d => d.CaHash,
                opt => opt.MapFrom(g => g.CaHash))
            .ForMember(d => d.CreateChainId,
                opt => opt.MapFrom(g => g.CreateChainId));
        //end

        CreateMap<RegisterRequestDto, RegisterDto>().BeforeMap((src, dest) =>
            {
                dest.ManagerInfo = new Account.ManagerInfo();
                dest.GuardianInfo = new GuardianInfo
                {
                    VerificationInfo = new VerificationInfo()
                };
                dest.Id = Guid.NewGuid();
            })
            .ForPath(t => t.ManagerInfo.Address, m => m.MapFrom(f => f.Manager))
            .ForPath(t => t.ManagerInfo.ExtraData, m => m.MapFrom(f => f.ExtraData))
            .ForPath(t => t.GuardianInfo.Type, m => m.MapFrom(f => (GuardianType)(int)f.Type))
            .ForPath(t => t.GuardianInfo.IdentifierHash, m => m.MapFrom(f => f.LoginGuardianIdentifier))
            .ForPath(t => t.GuardianInfo.VerificationInfo.Id, m => m.MapFrom(f => f.VerifierId))
            .ForPath(t => t.GuardianInfo.VerificationInfo.VerificationDoc, m => m.MapFrom(f => f.VerificationDoc))
            .ForPath(t => t.GuardianInfo.VerificationInfo.VerificationDoc, m => m.MapFrom(f => f.VerificationDoc))
            .ForPath(t => t.GuardianInfo.VerificationInfo.Signature, m => m.MapFrom(f => f.Signature));

        CreateMap<RecoveryRequestDto, RecoveryDto>().BeforeMap((src, dest) =>
            {
                dest.ManagerInfo = new Account.ManagerInfo();
                dest.GuardianApproved = new List<GuardianInfo>();
                dest.Id = Guid.NewGuid();
            })
            .ForPath(t => t.ManagerInfo.Address, m => m.MapFrom(f => f.Manager))
            .ForPath(t => t.ManagerInfo.ExtraData, m => m.MapFrom(f => f.ExtraData))
            .ForPath(t => t.GuardianApproved, m => m.MapFrom(f => f.GuardiansApproved.Select(
                t => new GuardianInfo
                {
                    Type = (GuardianType)(int)t.Type,
                    IdentifierHash = t.Identifier,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = t.VerifierId,
                        VerificationDoc = t.VerificationDoc,
                        Signature = t.Signature
                    }
                }).ToList()));

        CreateMap<Entities.Es.ContactAddress, UserContactAddressDto>();
        CreateMap<AppleUserExtraInfo, UserExtraInfoGrainDto>();
        CreateMap<GoogleUserExtraInfo, UserExtraInfoGrainDto>();
        // CreateMap<TelegramUserExtraInfo, UserExtraInfoGrainDto>();
        CreateMap<GoogleUserExtraInfo, Verifier.Dtos.UserExtraInfo>();
        CreateMap<AppleUserExtraInfo, Verifier.Dtos.UserExtraInfo>();
        CreateMap<TelegramUserExtraInfo, Verifier.Dtos.UserExtraInfo>()
            .ForMember(t => t.FullName, m => m.MapFrom(f => f.UserName))
            .ForMember(t => t.Picture, m => m.MapFrom(f => f.ProtoUrl));
        CreateMap<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>();
        CreateMap<UserExtraInfoGrainDto, UserExtraInfoEto>();
        CreateMap<UserExtraInfoGrainDto, UserExtraInfoResultDto>()
            .ForMember(t => t.IsPrivate, m => m.MapFrom(f => f.IsPrivateEmail));
        CreateMap<DefaultIpInfoOptions, IpInfoResultDto>();
        CreateMap<IpInfoDto, IpInfoResultDto>().ForMember(t => t.Country, m => m.MapFrom(f => f.CountryName))
            .ForMember(t => t.Code, m => m.MapFrom(f => f.CountryCode))
            .ForPath(t => t.Iso, m => m.MapFrom(f => f.Location.CallingCode));

        // third part order auto map
        CreateMap<CreateUserOrderDto, OrderGrainDto>();
        CreateMap<OrderGrainDto, OrderDto>();
        CreateMap<OrderDto, OrderGrainDto>();
        CreateMap<OrderGrainDto, OrderEto>();
        CreateMap<RampOrderIndex, OrderDto>();
        CreateMap<AlchemyOrderUpdateDto, OrderGrainDto>()
            .ForMember(t => t.FiatAmount, m => m.MapFrom(f => f.Amount))
            .ForMember(t => t.TransactionId, m => m.MapFrom(f => f.TxHash))
            .ForMember(t => t.PaymentMethod, m => m.MapFrom(f => f.PayType))
            .ForMember(t => t.ReceivingMethod, m => m.MapFrom(f => f.PaymentType))
            .ForMember(t => t.ThirdPartOrderNo, m => m.MapFrom(f => f.OrderNo));
        CreateMap<OrderDto, WaitToSendOrderInfoDto>()
            .ForMember(t => t.OrderNo, m => m.MapFrom(f => f.ThirdPartOrderNo))
            .ForMember(t => t.Network, m => m.MapFrom(f => f.ThirdPartNetwork))
            .ForMember(t => t.Crypto, m => m.MapFrom(f => f.ThirdPartCrypto));

        CreateMap<CreateNotifyDto, NotifyGrainDto>();
        CreateMap<UpdateNotifyDto, NotifyGrainDto>();
        CreateMap<NotifyGrainDto, NotifyResultDto>();
        CreateMap<NotifyGrainDto, DeleteNotifyEto>();
        CreateMap<NotifyGrainDto, NotifyEto>();
        CreateMap<NotifyGrainDto, PullNotifyResultDto>();

        CreateMap<ScanLoginDto, ScanLoginEto>().BeforeMap((src, dest) => { dest.Message = "Login Successful"; });

        CreateMap<CreateUserEto, CAHolderIndex>();
        CreateMap<Verifier.Dtos.UserExtraInfo, UserExtraInfoResultDto>();
        CreateMap<TransactionsDto, IndexerTransactions>()
            .ForMember(t => t.CaHolderTransaction, m => m.MapFrom(f => f.TwoCaHolderTransaction));

        CreateMap<CmsNotify, NotifyGrainDto>()
            .Ignore(t => t.Id)
            .ForMember(t => t.NotifyId, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.AppVersions,
                m => m.MapFrom(f => f.AppVersions == null ? null : f.AppVersions.Select(t => t.AppVersion.Value)))
            .ForMember(t => t.TargetVersion, m => m.MapFrom(f => f.TargetVersion == null ? "" : f.TargetVersion.Value))
            .ForMember(t => t.Countries,
                m => m.MapFrom(f => f.Countries == null ? null : f.Countries.Select(t => t.Country.Value)))
            .ForMember(t => t.DeviceBrands,
                m => m.MapFrom(f => f.DeviceBrands == null ? null : f.DeviceBrands.Select(t => t.DeviceBrand.Value)))
            .ForMember(t => t.OperatingSystemVersions,
                m => m.MapFrom(f =>
                    f.OperatingSystemVersions == null
                        ? null
                        : f.OperatingSystemVersions.Select(t => t.OperatingSystemVersion.Value)))
            .ForMember(t => t.DeviceTypes,
                m => m.MapFrom(f =>
                    f.DeviceTypes == null
                        ? null
                        : f.DeviceTypes.Select(t => ((DeviceType)t.DeviceType.Value).ToString())))
            .ForMember(t => t.StyleType, m => m.MapFrom(f => (StyleType)f.StyleType.Value));

        CreateMap<UserTokenIndex, GetTokenInfoDto>()
            .ForMember(t => t.IsDefault, m => m.MapFrom(f => f.IsDefault))
            .ForMember(t => t.IsDisplay, m => m.MapFrom(f => f.IsDisplay))
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id.ToString()))
            .ForPath(t => t.Symbol, m => m.MapFrom(f => f.Token.Symbol))
            .ForPath(t => t.ChainId, m => m.MapFrom(f => f.Token.ChainId))
            .ForPath(t => t.TokenContractAddress, m => m.MapFrom(f => f.Token.Address))
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals))
            .ForPath(t => t.ImageUrl, m => m.MapFrom(f => f.Token.ImageUrl));

        CreateMap<UserTokenIndex, GetTokenListDto>()
            .ForMember(t => t.IsDefault, m => m.MapFrom(f => f.IsDefault))
            .ForMember(t => t.IsDisplay, m => m.MapFrom(f => f.IsDisplay))
            .ForMember(t => t.Id,
                m => m.MapFrom(f => f.Id == Guid.Empty ? $"{f.Token.ChainId}-{f.Token.Symbol}" : f.Id.ToString()))
            .ForPath(t => t.Symbol, m => m.MapFrom(f => f.Token.Symbol))
            .ForPath(t => t.ChainId, m => m.MapFrom(f => f.Token.ChainId))
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals))
            .ForPath(t => t.ImageUrl, m => m.MapFrom(f => f.Token.ImageUrl));

        CreateMap<UserTokenIndex, GetUserTokenDto>()
            .ForMember(t => t.IsDefault, m => m.MapFrom(f => f.IsDefault))
            .ForMember(t => t.IsDisplay, m => m.MapFrom(f => f.IsDisplay))
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id.ToString()))
            .ForPath(t => t.Symbol, m => m.MapFrom(f => f.Token.Symbol))
            .ForPath(t => t.Address, m => m.MapFrom(f => f.Token.Address))
            .ForPath(t => t.ChainId, m => m.MapFrom(f => f.Token.ChainId))
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals))
            .ForPath(t => t.ImageUrl, m => m.MapFrom(f => f.Token.ImageUrl));

        CreateMap<UserTokenItem, GetUserTokenDto>()
            .ForMember(t => t.IsDefault, m => m.MapFrom(f => f.IsDefault))
            .ForMember(t => t.IsDisplay, m => m.MapFrom(f => f.IsDisplay))
            .ForMember(t => t.Id, m => m.MapFrom(f => $"{f.Token.ChainId}-{f.Token.Symbol}"))
            .ForPath(t => t.Symbol, m => m.MapFrom(f => f.Token.Symbol))
            .ForPath(t => t.Address, m => m.MapFrom(f => f.Token.Address))
            .ForPath(t => t.ChainId, m => m.MapFrom(f => f.Token.ChainId))
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals))
            .ForPath(t => t.ImageUrl, m => m.MapFrom(f => f.Token.ImageUrl));

        CreateMap<IndexerToken, GetTokenInfoDto>();
        CreateMap<IndexerToken, GetTokenListDto>();
        CreateMap<CreateBookmarkDto, BookmarkGrainDto>();
        CreateMap<BookmarkGrainResultDto, BookmarkCreateEto>();
        CreateMap<BookmarkIndex, BookmarkResultDto>();

        CreateMap<BookmarkCreateEto, BookmarkIndex>();
        CreateMap<BookmarkGrainDto, BookmarkResultDto>();
        CreateMap<PagedResultDto<BookmarkIndex>, PagedResultDto<BookmarkResultDto>>();
        CreateMap<IndexerToken, UserTokenItem>()
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(f => f.ChainId))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(f => f.Symbol))
            .ForPath(t => t.Token.Address, m => m.MapFrom(f => f.TokenContractAddress))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(f => f.Decimals))
            .ForPath(t => t.Token.ImageUrl, m => m.MapFrom(f => f.ImageUrl));

        CreateMap<TransactionDto, TransactionEto>();
        CreateMap<OrderStatusInfoGrainResultDto, OrderStatusInfoEto>();
        CreateMap<OrderGrainDto, OrderStatusInfoGrainDto>()
            .ForMember(t => t.Id, opt => opt.Ignore())
            .ForMember(t => t.OrderId, m => m.MapFrom(f => f.Id))
            .ForPath(t => t.OrderStatusInfo.Status,
                m => m.MapFrom(f => (OrderStatusType)Enum.Parse(typeof(OrderStatusType), f.Status)))
            .ForPath(t => t.OrderStatusInfo.LastModifyTime, m => m.MapFrom(f => Convert.ToInt64(f.LastModifyTime)));

        CreateMap<TransactionFeeInfo, TransactionFeeResultDto>();
        CreateMap<BookmarkGrainResultDto, BookmarkResultDto>();
        CreateMap<VerifierServer, GetVerifierServerResponse>()
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id.ToHex()));
        CreateMap<ContactIndex, ContactResultDto>()
            .ForMember(t => t.ModificationTime,
                m => m.MapFrom(f => TimeHelper.GetTimeStampFromDateTime(f.ModificationTime)))
            .ForMember(t => t.Name, f => f.MapFrom(m => m.Name ?? string.Empty))
            .ReverseMap();
        CreateMap<CAHolderIndex, CAHolderResultDto>();
        CreateMap<CAHolderIndex, CAHolderWithAddressResultDto>();
        CreateMap<ContactAddress, ContactAddressDto>();
        CreateMap<CreateUpdateContactDto, ContactDto>();
        CreateMap<ContactDto, ContactGrainDto>();
        CreateMap<CAHolderIndex, Contacts.CaHolderInfo>()
            .ForMember(t => t.WalletName, m => m.MapFrom(f => f.NickName));
        CreateMap<CAHolderGrainDto, Contacts.CaHolderInfo>()
            .ForMember(t => t.WalletName, m => m.MapFrom(f => f.Nickname));
        CreateMap<Entities.Es.CaHolderInfo, Contacts.CaHolderInfo>();
        CreateMap<Entities.Es.ImInfo, Contacts.ImInfo>();

        CreateMap<CAHolderGrainDto, DeleteCAHolderEto>();
        CreateMap<ImInfoDto, ImInfo>();
        CreateMap<AddressWithChain, ContactAddressDto>()
            .ForMember(t => t.ChainId, m => m.MapFrom(f => f.ChainName.ToUpper()))
            .ForMember(t => t.ChainName, f => f.MapFrom(m => m.ChainName));
        CreateMap<Contacts.CaHolderInfo, CaHolderDto>()
            .ForMember(t => t.UserId, m => m.MapFrom(f => f.UserId == Guid.Empty ? string.Empty : f.UserId.ToString()))
            ;
        CreateMap<ImInfo, ImInfos>()
            .ForMember(t => t.PortkeyId,
                m => m.MapFrom(f => f.PortkeyId == Guid.Empty ? string.Empty : f.PortkeyId.ToString()))
            ;
        CreateMap<ContactResultDto, ContactListDto>()
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id == Guid.Empty ? string.Empty : f.Id.ToString()))
            .ForMember(t => t.UserId, m => m.MapFrom(f => f.UserId == Guid.Empty ? string.Empty : f.UserId.ToString()))
            .ReverseMap()
            ;

        CreateMap<ValidateOriginChainIdGrainDto, ValidateOriginChainIdState>().ReverseMap();


        CreateMap<PrivacyPolicyIndex, PrivacyPolicyDto>().ReverseMap();
        CreateMap<PrivacyPolicySignDto, PrivacyPolicyDto>().ReverseMap();

        CreateMap<UserDeviceReportingRequestDto, UserDeviceReportingDto>();
        CreateMap<AppStatusReportingRequestDto, AppStatusReportingDto>();

        CreateMap<CAHolderIndex, HolderInfoWithAvatar>()
            .ForMember(t => t.WalletName, m => m.MapFrom(f => f.NickName));
        CreateMap<CAHolderGrainDto, HolderInfoWithAvatar>()
            .ForMember(t => t.WalletName, m => m.MapFrom(f => f.Nickname));
        CreateMap<HolderInfoWithAvatar, Contacts.CaHolderInfo>().ReverseMap();
        CreateMap<CAHolderIndex, HolderInfoResultDto>();

        CreateMap<CreateNftOrderRequestDto, OrderGrainDto>()
            .Ignore(des => des.MerchantName)
            .ForMember(des => des.Address, opt => opt.MapFrom(src => src.UserAddress))
            .ForMember(des => des.Crypto, opt => opt.MapFrom(src => src.PaymentSymbol))
            .ForMember(des => des.CryptoAmount, opt => opt.MapFrom(src => src.PaymentAmount));

        CreateMap<CreateNftOrderRequestDto, NftOrderGrainDto>().ReverseMap();
        CreateMap<OrderStatusInfoIndex, OrderStatusSection>().ReverseMap();


        CreateMap<OrderStatusInfoEto, OrderStatusInfoIndex>().ReverseMap();
        CreateMap<CAServer.ThirdPart.Dtos.OrderStatusInfo, CAServer.Entities.Es.OrderStatusInfo>().ReverseMap();

        CreateMap<OrderEto, RampOrderIndex>().ReverseMap();
        CreateMap<OrderEto, NotifyOrderDto>()
            .ForMember(des => des.OrderId, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<RampOrderIndex, NotifyOrderDto>()
            .ForMember(des => des.OrderId, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<OrderGrainDto, NotifyOrderDto>()
            .ForMember(des => des.OrderId, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<NftOrderIndex, NftOrderSectionDto>()
            .ForMember(des => des.ExpireTime, opt => opt.MapFrom(src => src.ExpireTime.ToUtcMilliSeconds()))
            .ForMember(des => des.CreateTime, opt => opt.MapFrom(src => src.CreateTime.ToUtcMilliSeconds()));

        CreateMap<OrderSettlementGrainDto, OrderSettlementIndex>().ReverseMap();
        CreateMap<NftOrderGrainDto, NftOrderIndex>();
        CreateMap<NftOrderIndex, NftOrderQueryResponseDto>();
        CreateMap<NftOrderSectionDto, NftOrderQueryResponseDto>();
        CreateMap<OrderSettlementIndex, OrderSettlementSectionDto>().ReverseMap();
        CreateMap<OrderDto, NftOrderQueryResponseDto>()
            .ForMember(des => des.PaymentSymbol, opt => opt.MapFrom(src => src.Crypto))
            .ForMember(des => des.PaymentAmount, opt => opt.MapFrom(src => src.CryptoAmount));
        CreateMap<GuardianInfoBase, GuardianIndexerInfoDto>();
        CreateMap<Portkey.Contracts.CA.Guardian, GuardianIndexerInfoDto>()
            .ForMember(t => t.IdentifierHash, m => m.MapFrom(f => f.IdentifierHash.ToHex()))
            .ForMember(t => t.VerifierId, m => m.MapFrom(f => f.VerifierId.ToHex()));
        CreateMap<RedPackageIndex, RedPackageDetailDto>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => src.TotalAmount.ToString()))
            .ReverseMap()
            .ForMember(dest => dest.TotalAmount,
                opt => opt.MapFrom(src => long.Parse(src.TotalAmount)));
        CreateMap<RedPackageIndex, CryptoGiftHistoryItemDto>()
            .ForMember(dest => dest.Id, src => src.MapFrom(m => m.RedPackageId));
        CreateMap<PreGrabItem, PreGrabbedItemDto>()
            .ForMember(dest => dest.Username, src => src.MapFrom(m => "Pending Deposit"));
        CreateMap<PreGrabbedItemDto, GrabItemDto>();
        CreateMap<RedPackageDetailDto, CryptoGiftHistoryItemDto>()
            .ForMember(dest => dest.Label,
                src => src.MapFrom(m =>
                    ETransferConstant.SgrName.Equals(m.Symbol) ? ETransferConstant.SgrDisplayName : null))
            .ForMember(dest => dest.Exist, src => src.MapFrom(m => true))
            .ForMember(dest => dest.Decimals, src => src.MapFrom(m => m.Decimal))
            .ForMember(dest => dest.DisplayStatus,
                src => src.MapFrom(m => RedPackageDisplayStatus.GetDisplayStatus(m.Status)));
        CreateMap<CAServer.Entities.Es.Token, CAServer.Search.Dtos.Token>();
        CreateMap<UserTokenIndex, UserTokenIndexDto>()
            .ForMember(t => t.Token, m => m.MapFrom(src => src.Token));
        CreateMap<ImTransferDto, TransferGrainDto>();
        CreateMap<TransferGrainDto, TransferIndex>();
        CreateMap<TransferIndex, TransferResultDto>().ForMember(t => t.Status,
            m => m.MapFrom(f => Enum.Parse(typeof(TransferTransactionStatus), f.TransactionStatus)));
        CreateMap<TransferIndex, TransferEto>().ReverseMap();


        CreateMap<ThirdPartProvider, RampCoverageDto>().ReverseMap();
        CreateMap<CryptoItem, RampCurrencyItem>().ReverseMap();
        CreateMap<AlchemyOrderQuoteDataDto, RampPriceDto>()
            .ForMember(des => des.FiatAmount, opt => opt.MapFrom(src => src.FiatQuantity))
            .ForMember(des => des.CryptoAmount, opt => opt.MapFrom(src => src.CryptoQuantity))
            .ForMember(des => des.Exchange, opt => opt.MapFrom(src => src.CryptoPrice))
            .ReverseMap();
        CreateMap<AlchemyOrderQuoteDataDto, ProviderRampDetailDto>()
            .ForMember(des => des.FiatAmount, opt => opt.MapFrom(src => src.FiatQuantity))
            .ForMember(des => des.CryptoAmount, opt => opt.MapFrom(src => src.CryptoQuantity))
            .ForMember(des => des.Exchange, opt => opt.MapFrom(src => src.CryptoPrice))
            .ReverseMap();
        CreateMap<RampDetailRequest, GetAlchemyOrderQuoteDto>()
            .Ignore(des => des.Type)
            .ForMember(des => des.Side, opt => opt.MapFrom(src => src.Type))
            .ForMember(des => des.Amount,
                opt => opt.MapFrom(src =>
                    src.Type == OrderTransDirect.BUY.ToString() ? src.FiatAmount : src.CryptoAmount))
            .ReverseMap();
        CreateMap<RampExchangeRequest, GetAlchemyOrderQuoteDto>()
            .Ignore(des => des.Type)
            .ForMember(des => des.Side, opt => opt.MapFrom(src => src.Type))
            .ReverseMap();
        CreateMap<RampLimitRequest, GetAlchemyOrderQuoteDto>()
            .Ignore(des => des.Type)
            .ForMember(des => des.Side, opt => opt.MapFrom(src => src.Type))
            .ReverseMap();
        CreateMap<TransakRampPrice, RampPriceDto>()
            .ForMember(des => des.Exchange, opt => opt.MapFrom(src => src.FiatCryptoExchange()))
            .ReverseMap();
        CreateMap<TransakRampPrice, ProviderRampDetailDto>()
            .ForMember(des => des.Exchange, opt => opt.MapFrom(src => src.FiatCryptoExchange()))
            .ForMember(des => des.ProviderNetwork, opt => opt.MapFrom(src => src.Network))
            .ForMember(des => des.ProviderSymbol, opt => opt.MapFrom(src => src.CryptoCurrency))
            .ReverseMap();
        CreateMap<RampExchangeRequest, RampDetailRequest>().ReverseMap();
        CreateMap<RampLimitRequest, RampDetailRequest>().ReverseMap();
        CreateMap<RampPriceDto, ProviderRampDetailDto>().ReverseMap();
        CreateMap<RampFiatItem, DefaultFiatCurrency>().ReverseMap();
        CreateMap<ThirdPartProvider, RampProviderDto>()
            .ForMember(des => des.CallbackUrl, opt => opt.MapFrom(src => src.WebhookUrl))
            .ReverseMap();
        CreateMap<ProviderCoverage, RampProviderCoverageDto>()
            .ForMember(des => des.Buy, opt => opt.MapFrom(src => src.OnRamp))
            .ForMember(des => des.Sell, opt => opt.MapFrom(src => src.OffRamp))
            .ReverseMap();
        CreateMap<RampDetailRequest, GetRampPriceRequest>()
            .ForMember(des => des.IsBuyOrSell, opt => opt.MapFrom(src => src.Type))
            .ForMember(des => des.FiatCurrency, opt => opt.MapFrom(src => src.Fiat))
            .ForMember(des => des.CryptoCurrency, opt => opt.MapFrom(src => src.Crypto))
            .ReverseMap();
        CreateMap<TransakOrderDto, OrderDto>()
            .ForMember(t => t.Id, m => m.MapFrom(f => Guid.Parse(f.PartnerOrderId)))
            .ForMember(t => t.ThirdPartOrderNo, m => m.MapFrom(f => f.Id))
            .ForMember(t => t.TransDirect, m
                => m.MapFrom(f =>
                    f.IsBuy() ? TransferDirectionType.TokenBuy.ToString() : TransferDirectionType.TokenSell.ToString()))
            .ForMember(t => t.Address, m => m.MapFrom(f => f.WalletAddress))
            .ForMember(t => t.Crypto, m => m.MapFrom(f => f.Cryptocurrency))
            .ForMember(t => t.CryptoAmount, m => m.MapFrom(f => f.CryptoAmount))
            .ForMember(t => t.Fiat, m => m.MapFrom(f => f.FiatCurrency))
            .ForMember(t => t.FiatAmount, m => m.MapFrom(f => f.FiatAmount))
            .ForMember(t => t.Status, m => m.MapFrom(f => f.Status))
            ;
        CreateMap<QueryAlchemyOrderInfo, OrderDto>()
            .ForMember(t => t.Id, m => m.MapFrom(f => Guid.Parse(f.MerchantOrderNo)))
            .ForMember(t => t.ThirdPartOrderNo, m => m.MapFrom(f => f.OrderNo))
            .ForMember(t => t.TransactionId, m => m.MapFrom(f => f.TxHash))
            .ReverseMap();

        CreateMap<TelegramAuthReceiveRequest, TelegramAuthDto>()
            .ForMember(t => t.AuthDate, m => m.MapFrom(f => f.Auth_Date))
            .ForMember(t => t.FirstName, m => m.MapFrom(f => f.First_Name))
            .ForMember(t => t.LastName, m => m.MapFrom(f => f.Last_Name))
            .ForMember(t => t.PhotoUrl, m => m.MapFrom(f => f.Photo_Url));

        CreateMap<UserExtraInfoResultDto, Verifier.Dtos.UserExtraInfo>()
            .ForMember(t => t.IsPrivateEmail, m => m.MapFrom(f => f.IsPrivate));

        CreateMap<AlchemyTreasuryOrderRequestDto, TreasuryOrderRequest>()
            .ForMember(des => des.ThirdPartOrderId, opt => opt.MapFrom(src => src.OrderNo))
            .ReverseMap();

        CreateMap<TreasuryOrderRequest, TreasuryOrderDto>()
            .ForMember(des => des.ToAddress, opt => opt.MapFrom(src => src.Address))
            .ForMember(des => des.CryptoPriceInUsdt, opt => opt.MapFrom(src => src.CryptoPrice))
            .ForMember(des => des.SettlementAmount, opt => opt.MapFrom(src => src.UsdtAmount))
            .ReverseMap();

        CreateMap<AlchemyTreasuryOrderRequestDto, TreasuryOrderDto>()
            .ForMember(des => des.ThirdPartOrderId, opt => opt.MapFrom(src => src.OrderNo))
            .ForMember(des => des.CryptoPriceInUsdt, opt => opt.MapFrom(src => src.CryptoPrice))
            .ForMember(des => des.SettlementAmount, opt => opt.MapFrom(src => src.UsdtAmount))
            .ReverseMap();

        CreateMap<TreasuryOrderDto, TreasuryOrderIndex>().ReverseMap();
        CreateMap<PendingTreasuryOrderIndex, PendingTreasuryOrderDto>().ReverseMap();

        CreateMap<RedDotGrainDto, RedDotEto>();
        CreateMap<GrowthGrainDto, CreateGrowthEto>();
        CreateMap<RedDotInfo, RedDotInfoDto>();

        CreateMap<UpgradeInfoIndex, UpgradeResponseDto>();
        CreateMap<UpgradeGrainDto, CreateUpgradeInfoEto>();
        CreateMap<CreateUpgradeInfoEto, UpgradeInfoIndex>();

        CreateMap<GuideInfo, UserGuideInfo>().ForMember(t => t.GuideType, m => m.MapFrom(f => (GuideType)f.GuideType));
        CreateMap<UserGuideInfo, UserGuideInfoGrainDto>();
        CreateMap<UserGuideInfoGrainDto, UserGuideInfo>();

        CreateMap<UserExtraInfoResultDto, Verifier.Dtos.UserExtraInfo>()
            .ForMember(t => t.IsPrivateEmail, m => m.MapFrom(f => f.IsPrivate));
        CreateMap<FacebookUserInfoDto, Verifier.Dtos.UserExtraInfo>().ReverseMap();
        CreateMap<RampCurrencyItem, DefaultCryptoCurrency>().ReverseMap();
        CreateMap<FacebookUserInfoDto, Verifier.Dtos.UserExtraInfo>().ReverseMap();

        CreateMap<TwitterUserExtraInfo, Verifier.Dtos.UserExtraInfo>();
        CreateMap<TabCompleteDto, TabCompleteEto>();
        CreateMap<CAServer.Options.Token, CAServer.Entities.Es.Token>();
        CreateMap<UserTokenItem, UserTokenIndex>();
        CreateMap<CAServer.Options.Token, CAServer.Search.Dtos.Token>();
        CreateMap<UserTokenItem, UserTokenIndexDto>();
        CreateMap<AuthTokenRequestDto, ETransferAuthTokenRequestDto>().ForMember(des => des.ClientId,
                opt => opt.MapFrom(f => ETransferConstant.ClientId))
            .ForMember(des => des.GrantType,
                opt => opt.MapFrom(f => ETransferConstant.GrantType))
            .ForMember(des => des.Version,
                opt => opt.MapFrom(f => ETransferConstant.Version))
            .ForMember(des => des.Source,
                opt => opt.MapFrom(f => ETransferConstant.Source))
            .ForMember(des => des.Scope,
                opt => opt.MapFrom(f => ETransferConstant.Scope))
            ;
        CreateMap<TokenSpender, TokenAllowance>();
        CreateMap<CAHolderGrainDto, CAHolderIndex>();
        CreateMap<CoinMarkets, MarketCryptocurrencyDto>()
            .ForMember(t => t.Symbol, s => s.MapFrom(m => m.Symbol.ToUpper()))
            .ForMember(t => t.OriginalMarketCap, s => s.MapFrom(m => m.MarketCap))
            .ForMember(t => t.OriginalCurrentPrice, s => s.MapFrom(m => m.CurrentPrice))
            .ForMember(t => t.PriceChangePercentage24H, s =>
                s.MapFrom(m =>
                    !m.PriceChangePercentage24H.HasValue
                        ? Decimal.Zero
                        : Math.Round((decimal)m.PriceChangePercentage24H, 1)))
            .ForMember(t => t.CurrentPrice, s =>
                s.MapFrom(m => (m.CurrentPrice == null || !m.CurrentPrice.HasValue)
                    ? Decimal.Zero
                    : Decimal.Compare((decimal)m.CurrentPrice, Decimal.One) >= 0
                        ? Math.Round((decimal)m.CurrentPrice, 2)
                        : Decimal.Compare((decimal)m.CurrentPrice, (decimal)0.1) >= 0
                            ? Math.Round((decimal)m.CurrentPrice, 4, MidpointRounding.ToZero)
                            : Decimal.Compare((decimal)m.CurrentPrice, (decimal)0.01) >= 0
                                ? Math.Round((decimal)m.CurrentPrice, 5, MidpointRounding.ToZero)
                                : Decimal.Compare((decimal)m.CurrentPrice, (decimal)0.001) >= 0
                                    ? Math.Round((decimal)m.CurrentPrice, 6, MidpointRounding.ToZero)
                                    : Decimal.Compare((decimal)m.CurrentPrice, (decimal)0.0001) >= 0
                                        ? Math.Round((decimal)m.CurrentPrice, 7, MidpointRounding.ToZero)
                                        : (decimal)m.CurrentPrice))
            .ForMember(t => t.MarketCap, s =>
                s.MapFrom(m => (m.MarketCap == null || !m.MarketCap.HasValue)
                    ? string.Empty
                    : (Decimal.Compare((decimal)m.MarketCap, 1000000000) > 0)
                        ? Decimal.Divide((decimal)m.MarketCap, 1000000000).ToString("0.00") + "B"
                        : (Decimal.Compare((decimal)m.MarketCap, 1000000) > 0)
                            ? Decimal.Divide((decimal)m.MarketCap, 1000000).ToString("0.00") + "M"
                            : m.MarketCap.ToString()));
        CreateMap<TransactionReportDto, TransactionReportEto>();
        CreateMap<CaHolderTransactionIndex, IndexerTransaction>();
        CreateMap<ActivityConfig, ActivityConfigDto>();
        CreateMap<RulesConfig, RulesConfigDto>();
        CreateMap<BeInvitedConfig, BeInvitedConfigDto>();
        CreateMap<TaskConfigInfo, TaskConfig>();
        CreateMap<NoticeInfo, Notice>();

        CreateMap<ConfirmRequestDto, ConfirmGrainDto>();
        CreateMap<FreeMintIndex, GetItemInfoDto>();
        CreateMap<AccountReportDto, AccountReportEto>();
        CreateMap<GrowthIndex, GrowthUserInfoDto>();
        CreateMap<IndexerToken, GetUserTokenDto>()
            .ForMember(t => t.Address, m => m.MapFrom(f => f.TokenContractAddress));
        CreateMap<VerifiedZkLoginRequestDto, VerifyTokenRequestDto>();
        CreateMap<TokenInfoDto, TokenInfoV2Dto>();
        CreateMap<SearchUserAssetsRequestDto, GetNftCollectionsRequestDto>();
        CreateMap<ChainsInfoIndex, ChainResultDto>();
        CreateMap<CAServer.Entities.Es.DefaultTokenInfo, CAServer.Chain.DefaultToken>();
        CreateMap<FreeMintCollectionInfo, FreeMintCollectionInfoDto>();
        CreateMap<CAHolderIndex, AddressBook.Dtos.ContactCaHolderInfo>()
            .ForMember(t => t.WalletName, m => m.MapFrom(f => f.NickName));
        CreateMap<AddressBookDto, AddressBookGrainDto>().ReverseMap();
        CreateMap<AddressBookGrainDto, AddressBookEto>();
        CreateMap<AddressBookIndex, AddressBookDto>();
        CreateMap<AddressInfo, ContactAddressInfoDto>().ReverseMap();
        CreateMap<ContactAddressInfoDto, ContactAddressInfo>().ReverseMap();
        CreateMap<AddressInfo, ContactAddressInfo>().ReverseMap();
        CreateMap<CAServer.Entities.Es.ContactCaHolderInfo, CAServer.AddressBook.Dtos.ContactCaHolderInfo>().ReverseMap();
        CreateMap<CAServer.Entities.Es.ContactCaHolderInfo, ContactAddressInfoDto>().ReverseMap();
    }
}