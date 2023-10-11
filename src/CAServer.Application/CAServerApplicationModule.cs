using System.IdentityModel.Tokens.Jwt;
using CAServer.AccountValidator;
using CAServer.AppleAuth;
using CAServer.Common;
using CAServer.Grains;
using CAServer.Grains.Grain.ValidateOriginChainId;
using CAServer.IpInfo;
using CAServer.Monitor;
using CAServer.Options;
using CAServer.Search;
using CAServer.Settings;
using CAServer.Signature;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        Configure<GoogleRecaptchaOptions>(configuration.GetSection("GoogleRecaptcha"));
        Configure<AddToWhiteListUrlsOptions>(configuration.GetSection("AddToWhiteListUrls"));
        Configure<AppleTransferOptions>(configuration.GetSection("AppleTransfer"));
        Configure<ImServerOptions>(configuration.GetSection("ImServer"));
        Configure<HostInfoOptions>(configuration.GetSection("HostInfo"));
        Configure<SeedImageOptions>(configuration.GetSection("SeedSymbolImage"));
        Configure<SyncOriginChainIdOptions>(configuration.GetSection("SyncOriginChainId"));
        Configure<SecurityOptions>(configuration.GetSection("Security"));
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


        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<DeviceOptions>(configuration.GetSection("EncryptionInfo"));
        Configure<ActivitiesIcon>(configuration.GetSection("ActivitiesIcon"));
        Configure<AdaptableVariableOptions>(configuration.GetSection("AdaptableVariableSetting"));
        context.Services.AddSingleton<IAccountValidator, EmailValidator>();
        context.Services.AddSingleton<IAccountValidator, PhoneValidator>();
        //Configure<IndexPrefixOptions>(configuration.GetSection("IndexPrefixSetting"));
        Configure<IpServiceSettingOptions>(configuration.GetSection("IpServiceSetting"));
        Configure<AppleAuthOptions>(configuration.GetSection("AppleAuth"));
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
        Configure<ContractOptions>(configuration.GetSection("ContractOptions"));
        Configure<EsIndexBlacklistOptions>(configuration.GetSection("EsIndexBlacklist"));
        Configure<AwsThumbnailOptions>(configuration.GetSection("AWSThumbnail"));
        Configure<ActivityOptions>(configuration.GetSection("ActivityOptions"));
        context.Services.AddHttpClient();
        context.Services.AddScoped<JwtSecurityTokenHandler>();
        context.Services.AddScoped<IIpInfoClient, IpInfoClient>();
        context.Services.AddScoped<IHttpClientService, HttpClientService>();
        context.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        Configure<VariablesOptions>(configuration.GetSection("Variables"));
        context.Services.AddScoped<IImRequestProvider, ImRequestProvider>();
        Configure<VerifierIdMappingOptions>(configuration.GetSection("VerifierIdMapping"));
        Configure<VerifierAccountOptions>(configuration.GetSection("VerifierAccountDic"));
    }
}