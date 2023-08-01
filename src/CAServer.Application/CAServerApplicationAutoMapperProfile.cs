using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CAServer.Bookmark.Dtos;
using CAServer.Bookmark.Etos;
using CAServer.CAAccount.Dtos;
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
using CAServer.Grains.Grain.Bookmark.Dtos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.Notify;
using CAServer.Grains.Grain.ThirdPart;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Hubs;
using CAServer.IpInfo;
using CAServer.Message.Dtos;
using CAServer.Message.Etos;
using CAServer.Notify.Dtos;
using CAServer.Notify.Etos;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.Etos;
using CAServer.Tokens.Provider;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo.Dtos;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Portkey.Contracts.CA;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AutoMapper;
using ContactAddress = CAServer.Grains.Grain.Contacts.ContactAddress;
using GuardianInfo = CAServer.Account.GuardianInfo;
using GuardianType = CAServer.Account.GuardianType;
using Token = CAServer.UserAssets.Dtos.Token;
using VerificationInfo = CAServer.Account.VerificationInfo;

namespace CAServer;

public class CAServerApplicationAutoMapperProfile : Profile
{
    public CAServerApplicationAutoMapperProfile()
    {
        CreateMap<UserTokenGrainDto, UserTokenEto>();
        CreateMap<UserTokenGrainDto, UserTokenDeleteEto>();
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
        CreateMap<GuardianGrainDto, GuardianEto>();

        CreateMap<ManagerInfo, ManagerInfoDto>()
            .ForMember(t => t.Address, m => m.MapFrom(f => f.Address.ToBase58()));
        CreateMap<Portkey.Contracts.CA.Guardian, GuardianDto>()
            .ForMember(t => t.IdentifierHash, m => m.MapFrom(f => f.IdentifierHash.ToHex()))
            .ForMember(t => t.VerifierId, m => m.MapFrom(f => f.VerifierId.ToHex()))
            .ForMember(t => t.Type, m => m.MapFrom(f => (GuardianIdentifierType)(int)f.Type));

        CreateMap<GuardianList, GuardianListDto>();

        CreateMap<GetHolderInfoOutput, GuardianResultDto>()
            .ForMember(t => t.CaHash, m => m.MapFrom(f => f.CaHash.ToHex()))
            .ForMember(t => t.CaAddress, m => m.MapFrom(f => f.CaAddress.ToBase58()));
        // .ForPath(t => t.GuardianList, m => m.MapFrom(f => f.GuardianList.Guardians));

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
        CreateMap<GoogleUserExtraInfo, Verifier.Dtos.UserExtraInfo>();
        CreateMap<AppleUserExtraInfo, Verifier.Dtos.UserExtraInfo>();
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
            .ForMember(t => t.PaymentMethod, m => m.MapFrom(f => f.PayType))
            .ForMember(t => t.ReceivingMethod, m => m.MapFrom(f => f.PaymentType))
            .ForMember(t => t.ThirdPartOrderNo, m => m.MapFrom(f => f.OrderNo));
        CreateMap<OrderDto, WaitToSendOrderInfoDto>()
            .ForMember(t => t.OrderNo, m => m.MapFrom(f => f.ThirdPartOrderNo));

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
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals));

        CreateMap<UserTokenIndex, GetTokenListDto>()
            .ForMember(t => t.IsDefault, m => m.MapFrom(f => f.IsDefault))
            .ForMember(t => t.IsDisplay, m => m.MapFrom(f => f.IsDisplay))
            .ForMember(t => t.Id, m => m.MapFrom(f => f.Id.ToString()))
            .ForPath(t => t.Symbol, m => m.MapFrom(f => f.Token.Symbol))
            .ForPath(t => t.ChainId, m => m.MapFrom(f => f.Token.ChainId))
            .ForPath(t => t.Decimals, m => m.MapFrom(f => f.Token.Decimals));

        CreateMap<IndexerToken, GetTokenInfoDto>();
        CreateMap<IndexerToken, GetTokenListDto>();
        CreateMap<CreateBookmarkDto, BookmarkGrainDto>();
        CreateMap<BookmarkGrainResultDto, BookmarkCreateEto>();
        CreateMap<BookmarkIndex, BookmarkResultDto>();

        CreateMap<BookmarkCreateEto, BookmarkIndex>();
        CreateMap<BookmarkGrainDto, BookmarkResultDto>();
        CreateMap<PagedResultDto<BookmarkIndex>, PagedResultDto<BookmarkResultDto>>();
        CreateMap<IndexerToken, UserTokenItem>()
            .ForMember(t => t.SortWeight, m => m.MapFrom(f => f.ChainId == "AELF" ? 1 : 0))
            .ForPath(t => t.Token.ChainId, m => m.MapFrom(f => f.ChainId))
            .ForPath(t => t.Token.Symbol, m => m.MapFrom(f => f.Symbol))
            .ForPath(t => t.Token.Address, m => m.MapFrom(f => f.TokenContractAddress))
            .ForPath(t => t.Token.Decimals, m => m.MapFrom(f => f.Decimals));

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


        CreateMap<QueryAlchemyOrderInfo, OrderDto>()
            .ForMember(t => t.TransactionId, m => m.MapFrom(f => f.TxHash))
            .ForMember(t => t.Address, m => m.MapFrom(f => f.OrderAddress))
            .ForMember(t => t.CryptoAmount, m => m.MapFrom(f => f.CryptoActualAmount))
            .ForMember(t => t.PaymentMethod, m => m.MapFrom(f => f.PayType))
            .ForMember(t => t.ReceivingMethod, m => m.MapFrom(f => f.PaymentType))
            .ForMember(t => t.ThirdPartOrderNo, m => m.MapFrom(f => f.OrderNo))
            .ForMember(t => t.Id, m => m.MapFrom(f => Guid.Parse(f.MerchantOrderNo)))
            ;


    }
}