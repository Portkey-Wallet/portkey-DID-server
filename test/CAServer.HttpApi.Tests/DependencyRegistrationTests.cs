using System.Linq;
using CAServer.Cache;
using CAServer.CAAccount;
using CAServer.Google;
using CAServer.IpInfo;
using CAServer.IpWhiteList;
using CAServer.Options;
using CAServer.Switch;
using CAServer.Verifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace CAServer.HttpApi.Tests;

public class DependencyRegistrationTests
{
    [Fact]
    public void Email_RateLimit_Feature_Types_Should_Not_Implement_ITransientDependency()
    {
        var featureTypes = new[]
        {
            typeof(RegistrationEmailRateLimitService),
            typeof(HttpClientIpResolver),
            typeof(VerificationRequestOperationDispatcher),
            typeof(VerificationRequestRiskControlService),
            typeof(CreateCaHolderVerificationRequestHandler),
            typeof(SocialRecoveryVerificationRequestHandler)
        };

        foreach (var featureType in featureTypes)
        {
            Assert.DoesNotContain(typeof(ITransientDependency), featureType.GetInterfaces());
        }
    }

    [Fact]
    public void Email_RateLimit_Feature_Dependencies_Should_Be_Resolvable_From_Manual_Transient_Registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        services.AddSingleton<IOptions<RealIpOptions>>(Microsoft.Extensions.Options.Options.Create(new RealIpOptions
        {
            HeaderKey = ClientIpHeaders.XForwardedFor,
            AllowLegacyForwardedFallback = true
        }));
        services.AddSingleton<IOptionsSnapshot<RegistrationEmailRateLimitOptions>>(
            new StaticOptionsSnapshot<RegistrationEmailRateLimitOptions>(new RegistrationEmailRateLimitOptions()));
        services.AddSingleton(Mock.Of<ICacheProvider>());
        services.AddSingleton(Mock.Of<ICurrentUser>());
        services.AddSingleton(Mock.Of<IGoogleAppService>());
        services.AddSingleton(Mock.Of<IIpWhiteListAppService>());
        services.AddSingleton(Mock.Of<ISecondaryEmailAppService>());
        services.AddSingleton(Mock.Of<ISwitchAppService>());
        services.AddSingleton(Mock.Of<IVerifierAppService>());

        services.AddTransient<IRegistrationEmailRateLimitService, RegistrationEmailRateLimitService>();
        services.AddTransient<IHttpClientIpResolver, HttpClientIpResolver>();
        services.AddTransient<IVerificationRequestRiskControlService, VerificationRequestRiskControlService>();
        services.AddTransient<IVerificationRequestOperationDispatcher, VerificationRequestOperationDispatcher>();
        services.AddTransient<IVerificationRequestOperationHandler, CreateCaHolderVerificationRequestHandler>();
        services.AddTransient<IVerificationRequestOperationHandler, SocialRecoveryVerificationRequestHandler>();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<RegistrationEmailRateLimitService>(
            serviceProvider.GetRequiredService<IRegistrationEmailRateLimitService>());
        Assert.IsType<HttpClientIpResolver>(serviceProvider.GetRequiredService<IHttpClientIpResolver>());
        Assert.IsType<VerificationRequestRiskControlService>(
            serviceProvider.GetRequiredService<IVerificationRequestRiskControlService>());
        Assert.IsType<VerificationRequestOperationDispatcher>(
            serviceProvider.GetRequiredService<IVerificationRequestOperationDispatcher>());

        var handlers = serviceProvider.GetServices<IVerificationRequestOperationHandler>().ToList();

        Assert.Equal(2, handlers.Count);
        Assert.Single(handlers.OfType<CreateCaHolderVerificationRequestHandler>());
        Assert.Single(handlers.OfType<SocialRecoveryVerificationRequestHandler>());
    }

    private sealed class StaticOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        public StaticOptionsSnapshot(TOptions value)
        {
            Value = value;
        }

        public TOptions Value { get; }

        public TOptions Get(string? name)
        {
            return Value;
        }
    }
}
