﻿using System;
using System.IdentityModel.Tokens.Jwt;
using CAServer.AccountValidator;
using CAServer.AppleAuth;
using CAServer.Common;
using CAServer.Commons;
using CAServer.DataReporting;
using CAServer.Grains;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.RedPackage;
using CAServer.Search;
using CAServer.Settings;
using CAServer.Signature;
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
        Configure<SeedImageOptions>(configuration.GetSection("SeedSymbolImage"));
        Configure<SecurityOptions>(configuration.GetSection("Security"));
        Configure<FireBaseAppCheckOptions>(configuration.GetSection("FireBaseAppCheck"));
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
        Configure<RedPackageOptions>(configuration.GetSection("RedPackage"));
        context.Services.AddHttpClient();
        ConfigureRetryHttpClient(context.Services);
        context.Services.AddScoped<JwtSecurityTokenHandler>();
        context.Services.AddScoped<IIpInfoClient, IpInfoClient>();
        context.Services.AddScoped<IHttpClientService, HttpClientService>();
        context.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        Configure<VariablesOptions>(configuration.GetSection("Variables"));
        context.Services.AddScoped<IImRequestProvider, ImRequestProvider>();
        Configure<VerifierIdMappingOptions>(configuration.GetSection("VerifierIdMapping"));
        Configure<VerifierAccountOptions>(configuration.GetSection("VerifierAccountDic"));
        Configure<MessagePushOptions>(configuration.GetSection("MessagePush"));
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