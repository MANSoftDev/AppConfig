using Azure.Core;
using Azure.Identity;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace AppConfig.Extension
{
    /// <summary>  
    /// Extension class to facilitate configuring an application to use Azure App Configuration  
    /// </summary>  
    public static class WebApplicationBuilderExtension
    {
        public static WebApplicationBuilder AddAzureAppConfiguration(
            this WebApplicationBuilder builder,
            string appConfigEndpointKey,
            string sentinelKey,
            string keyFilter,
            string labelFilter)
        {
            builder.Services.AddFeatureManagement();

            var appConfigConnection = builder.Configuration[appConfigEndpointKey];
            if (!string.IsNullOrEmpty(appConfigConnection))
            {
                builder.Configuration.AddAzureAppConfiguration(options =>
                {
                    TokenCredential creds = new DefaultAzureCredential();
                    options.Connect(new Uri(builder.Configuration[appConfigEndpointKey]!), creds)
                        .Select(keyFilter, labelFilter)
                        .UseFeatureFlags(_ => _.SetRefreshInterval(TimeSpan.FromSeconds(5)))
                        .ConfigureRefresh(options =>
                        {
                            // All configuration values will be refreshed if the sentinel key changes.
                            options
                            .Register(sentinelKey, labelFilter, true)
                            .SetRefreshInterval(TimeSpan.FromSeconds(5));
                        })
                        .ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(creds);
                        });

                    AppConfiguration.Refresher = options.GetRefresher();
                });

                if (AppConfiguration.Refresher != null)
                {
                    builder.Services.AddSingleton(AppConfiguration.Refresher);
                }
            }
            else
            {
                throw new ArgumentNullException(nameof(appConfigEndpointKey), "App Configuration endpoint is not set.");
            }

            return builder;
        }
    }
}
