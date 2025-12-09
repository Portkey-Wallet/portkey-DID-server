using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;
using Volo.Abp;

namespace CAServer.Commons;

public static class ConfigurationProvidersHelper
{
    /// <summary>
    /// The Method displays the enabled configuration providers in the order they were added,
    /// configuration providers that are added later have higher priority and override previous key settings.
    /// </summary>
    /// <param name="context"></param>
    public static void DisplayConfigurationProviders(ApplicationInitializationContext context)
    {
        try
        {
            var configuration = context.GetConfiguration();
            var configurationRoot = (IConfigurationRoot)configuration;
            foreach (var provider in configurationRoot.Providers.ToList())
            {
                Log.Warning("ConfigurationProvider: {0}", provider.ToString());
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "display configuration providers error.");
        }
    }
}