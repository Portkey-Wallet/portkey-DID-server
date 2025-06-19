using System;
using System.IdentityModel.Tokens.Jwt;
using CAServer.AccountValidator;
using CAServer.Amazon;
using CAServer.AppleAuth;
using CAServer.CAAccount.Provider;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Common.AelfClient;
using CAServer.Commons;
using CAServer.CryptoGift;
using CAServer.DataReporting;
using CAServer.Facebook;
using CAServer.Grains;
using CAServer.IpInfo;
using CAServer.Market;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.Search;
using CAServer.Settings;
using CAServer.Signature;
using CAServer.ThirdPart.Adaptor;
using CAServer.ThirdPart.Alchemy;
using CAServer.ThirdPart.Processor;
using CAServer.ThirdPart.Processor.NFT;
using CAServer.ThirdPart.Processor.Ramp;
using CAServer.ThirdPart.Processors;
using CAServer.ThirdPart.Transak;
using CAServer.Tokens.Provider;
using CAServer.Telegram.Options;
using CAServer.ThirdPart.Processor.Treasury;
using CAServer.TwitterAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.DistributedLocking;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace CAServer;

[DependsOn(
    typeof(CAServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(CAServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(CAServerGrainsModule),
    typeof(CAServerSignatureModule),
    typeof(AbpDistributedLockingModule)
)]
public class CAServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<TokenListOptions>(configuration.GetSection("Tokens"));
        Configure<TokenInfoOptions>(configuration.GetSection("TokenInfo"));
        Configure<AssetsInfoOptions>(configuration.GetSection("AssetsInfo"));
        Configure<GoogleRecaptchaOptions>(configuration.GetSection("GoogleRecaptcha"));
        Configure<AddToWhiteListUrlsOptions>(configuration.GetSection("AddToWhiteListUrls"));
        Configure<AppleTransferOptions>(configuration.GetSection("AppleTransfer"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        Configure<HostInfoOptions>(configuration.GetSection("HostInfo"));
        Configure<AwsS3Option>(configuration.GetSection("AwsS3"));
        Configure<UserProfilePictureOptions>(configuration.GetSection("UserPictures"));
        Configure<MarketCacheOptions>(configuration.GetSection("MarketCache"));
        Configure<CryptoGiftOptions>(configuration.GetSection("CryptoGiftExpiration"));
        Configure<LoginCacheOptions>(configuration.GetSection("LoginCache"));
        Configure<AwakenOptions>(configuration.GetSection("AwakenConfig"));
        Configure<SecondaryEmailOptions>(configuration.GetSection("SecondaryEmail"));
        
        Configure<SeedImageOptions>(configuration.GetSection("SeedSymbolImage"));
        Configure<SecurityOptions>(configuration.GetSection("Security"));
        Configure<FireBaseAppCheckOptions>(configuration.GetSection("FireBaseAppCheck"));
        Configure<StopRegisterOptions>(configuration.GetSection("StopRegister"));
        Configure<FacebookOptions>(configuration.GetSection("Facebook"));
        Configure<AccelerateManagerOptions>(configuration.GetSection("AccelerateManager"));
        Configure<IpfsOptions>(configuration.GetSection("Ipfs"));
        Configure<ContractServiceOptions>(configuration.GetSection("ContractService"));
        Configure<TokenSpenderOptions>(configuration.GetSection("TokenSpender"));
        Configure<DepositOptions>(configuration.GetSection("Deposit"));
        Configure<NftToFtOptions>(configuration.GetSection("NftToFt"));
        Configure<ChatBotOptions>(configuration.GetSection("ChatBot"));
        Configure<FreeMintOptions>(configuration.GetSection("FreeMint"));
        Configure<ActivityConfigOptions>(configuration.GetSection("ActivityConfigs"));
        Configure<BeInvitedConfigOptions>(configuration.GetSection("BeInvitedConfig"));
        Configure<HamsterOptions>(configuration.GetSection("Hamster"));
        Configure<AddTokenOptions>(configuration.GetSection("AddToken"));
        Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
        Configure<ContactMigrateOptions>(configuration.GetSection("ContactMigrate"));
        Configure<BusinessAlertOptions>(configuration.GetSection("BusinessAlert"));

        context.Services.AddMemoryCache();
        context.Services.AddSingleton(typeof(ILocalMemoryCache<>), typeof(LocalMemoryCache<>));

        context.Services.AddSingleton<AlchemyProvider>();
        context.Services.AddSingleton<ISearchService, UserTokenSearchService>();
        context.Services.AddSingleton<ISearchService, ContactSearchService>();
        context.Services.AddSingleton<ISearchService, ChainsInfoSearchService>();
        context.Services.AddSingleton<ISearchService, AccountRecoverySearchService>();
        context.Services.AddSingleton<ISearchService, AccountRegisterSearchService>();
        context.Services.AddSingleton<ISearchService, CAHolderSearchService>();
        context.Services.AddSingleton<ISearchService, OrderSearchService>();
        context.Services.AddSingleton<ISearchService, UserExtraInfoSearchService>();
        context.Services.AddSingleton<ISearchService, NotifySearchService>();
        context.Services.AddSingleton<ISearchService, GuardianSearchService>();
        context.Services.AddSingleton<ISearchService, GrowthSearchService>();
        context.Services.AddSingleton<ISearchService, AccelerateRegisterSearchService>();
        context.Services.AddSingleton<ISearchService, AccelerateRecoverySearchService>();
        context.Services.AddSingleton<IPreValidationStrategy, ZkLoginPreValidationProvider>();
        context.Services.AddSingleton<IPreValidationStrategy, SignaturePreValidationProvider>();
        context.Services.AddSingleton<AlchemyProvider>();
        context.Services.AddSingleton<TransakProvider>();

        context.Services.AddSingleton<IThirdPartAdaptor, AlchemyAdaptor>();
        context.Services.AddSingleton<IThirdPartAdaptor, TransakAdaptor>();

        context.Services.AddSingleton<AbstractRampOrderProcessor, TransakOrderProcessor>();
        context.Services.AddSingleton<AbstractRampOrderProcessor, AlchemyOrderProcessor>();

        context.Services.AddSingleton<IThirdPartNftOrderProcessor, AlchemyNftOrderProcessor>();
        context.Services.AddSingleton<IThirdPartTreasuryProcessor, AlchemyTreasuryProcessor>();

        context.Services.AddSingleton<IExchangeProvider, BinanceProvider>();
        context.Services.AddSingleton<IExchangeProvider, OkxProvider>();

        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<DeviceOptions>(configuration.GetSection("EncryptionInfo"));
        Configure<ActivitiesIcon>(configuration.GetSection("ActivitiesIcon"));
        Configure<AdaptableVariableOptions>(configuration.GetSection("AdaptableVariableSetting"));
        context.Services.AddSingleton<IAccountValidator, EmailValidator>();
        context.Services.AddSingleton<IAccountValidator, PhoneValidator>();
        //Configure<IndexPrefixOptions>(configuration.GetSection("IndexPrefixSetting"));
        Configure<IpServiceSettingOptions>(configuration.GetSection("IpServiceSetting"));
        Configure<AppleAuthOptions>(configuration.GetSection("AppleAuth"));
        Configure<AppleAuthTransferredOptions>(configuration.GetSection("AppleAuthTransferred"));
        Configure<ThirdPartOptions>(configuration.GetSection("ThirdPart"));
        Configure<DefaultIpInfoOptions>(configuration.GetSection("DefaultIpInfo"));
        Configure<ContractAddressOptions>(configuration.GetSection("ContractAddress"));
        Configure<AppleCacheOptions>(configuration.GetSection("AppleCache"));
        Configure<SwitchOptions>(configuration.GetSection("Switch"));
        Configure<SendVerifierCodeRequestLimitOptions>(configuration.GetSection("SendVerifierCodeRequestLimit"));
        Configure<PhoneInfoOptions>(configuration.GetSection("PhoneInfoOptions"));
        Configure<ClaimTokenWhiteListAddressesOptions>(configuration.GetSection("ClaimTokenWhiteListAddresses"));
        Configure<ClaimTokenInfoOptions>(configuration.GetSection("ClaimTokenInfo"));
        Configure<CmsConfigOptions>(configuration.GetSection("CmsConfig"));
        Configure<EsIndexBlacklistOptions>(configuration.GetSection("EsIndexBlacklist"));
        Configure<AwsThumbnailOptions>(configuration.GetSection("AWSThumbnail"));
        Configure<ActivityOptions>(configuration.GetSection("ActivityOptions"));
        Configure<ActivitiesStatusIconOptions>(configuration.GetSection("ActivitiesStatusIcon"));
        Configure<ActivitiesSourceIconOptions>(configuration.GetSection("ActivitiesSourceIcon"));
        Configure<ExchangeOptions>(configuration.GetSection("Exchange"));
        Configure<RedPackageOptions>(configuration.GetSection("RedPackage"));
        Configure<TelegramAuthOptions>(configuration.GetSection("TelegramAuth"));
        Configure<TelegramVerifierOptions>(configuration.GetSection("TelegramVerifier"));
        // Configure<JwtTokenOptions>(configuration.GetSection("JwtToken"));
        Configure<ManagerCountLimitOptions>(configuration.GetSection("ManagerCountLimit"));
        Configure<UserGuideInfoOptions>(configuration.GetSection("GuideInfo"));
        Configure<TwitterAuthOptions>(configuration.GetSection("TwitterAuth"));
        context.Services.AddHttpClient();
        ConfigureRetryHttpClient(context.Services);
        context.Services.AddScoped<JwtSecurityTokenHandler>();
        context.Services.AddScoped<IIpInfoClient, IpInfoClient>();
        context.Services.AddScoped<IHttpClientService, HttpClientService>();
        context.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        context.Services.AddSingleton<IContractClient, MainChainContractClient>();
        context.Services.AddSingleton<IContractClient, SideChainContractClient>();
        
        Configure<VariablesOptions>(configuration.GetSection("Variables"));
        context.Services.AddScoped<IImRequestProvider, ImRequestProvider>();
        Configure<MessagePushOptions>(configuration.GetSection("MessagePush"));
        Configure<GetBalanceFromChainOption>(configuration.GetSection("GetBalanceFromChain"));
        Configure<NftItemDisplayOption>(configuration.GetSection("NftItemDisplay"));
        Configure<GrowthOptions>(configuration.GetSection("Growth"));
        Configure<PortkeyV1Options>(configuration.GetSection("PortkeyV1"));
        Configure<ETransferOptions>(configuration.GetSection("ETransfer"));
        Configure<ActivityDateRangeOptions>(configuration.GetSection("ETransfer"));
        
        AddMessagePushService(context, configuration);
    }

    private void AddMessagePushService(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var baseUrl = configuration["MessagePush:BaseUrl"];
        var appId = configuration["MessagePush:AppId"];
        if (baseUrl.IsNullOrWhiteSpace())
        {
            return;
        }

        context.Services.AddHttpClient(MessagePushConstant.MessagePushServiceName, httpClient =>
        {
            httpClient.BaseAddress = new Uri(baseUrl);

            if (!appId.IsNullOrWhiteSpace())
            {
                httpClient.DefaultRequestHeaders.Add(
                    "AppId", appId);
            }
        });
    }

    private void ConfigureRetryHttpClient(IServiceCollection services)
    {
        //if http code = 5xx or 408,this client will retry
        services.AddHttpClient(HttpConstant.RetryHttpClient)
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(
                    HttpConstant.RetryCount, retryNumber => TimeSpan.FromMilliseconds(HttpConstant.RetryDelayMs)));
    }
}