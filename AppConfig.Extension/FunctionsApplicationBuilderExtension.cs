using Azure.Core;
using Azure.Identity;

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace AppConfig.Extension
{
    /// <summary>  
    /// Extension class to facilitate configuring an application to use Azure App Configuration  
    /// </summary>  
    public static class FunctionsApplicationBuilderExtension
    {
        extension(FunctionsApplicationBuilder builder)
        {
            /// <summary>
            /// Configures the application to load settings from Azure App Configuration (with Key Vault
            /// references and feature flags). All inputs are resolved from configuration using the keys in
            /// <see cref="Constants"/>, so callers do not need to pass anything.
            /// </summary>
            /// <returns>The same <see cref="FunctionsApplicationBuilder"/> for chaining.</returns>
            public FunctionsApplicationBuilder AddAzureAppConfiguration()
            {
                builder.Services.AddFeatureManagement();

                var endpoint = builder.Configuration[Constants.APP_CONFIG_ENDPOINT];
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new ArgumentNullException(Constants.APP_CONFIG_ENDPOINT, "App Configuration endpoint is not set.");
                }

                var endpointUri = new Uri(endpoint);
                var label = builder.Configuration[Constants.ENVIRONMENT] ?? "Development";
                var keyFilter = builder.Configuration[Constants.KEY_FILTER]
                    ?? throw new InvalidOperationException($"'{Constants.KEY_FILTER}' is not configured.");
                var sentinelKey = builder.Configuration[Constants.SENTINEL_KEY]
                    ?? throw new InvalidOperationException($"'{Constants.SENTINEL_KEY}' is not configured.");

                // In Azure, ManagedIdentity is used. Locally, developer credentials (CLI/VS) are used.
                var isLocal = string.Equals(label, "Development", StringComparison.OrdinalIgnoreCase);

                builder.Configuration.AddAzureAppConfiguration(options =>
                {
                    TokenCredential creds = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        ExcludeWorkloadIdentityCredential = isLocal,
                        ExcludeManagedIdentityCredential = isLocal,
                        ExcludeInteractiveBrowserCredential = true,
                    });

                    options.Connect(endpointUri, creds)
                        .Select(keyFilter, label)
                        .UseFeatureFlags(flags => flags.SetRefreshInterval(TimeSpan.FromSeconds(5)))
                        .ConfigureRefresh(refresh =>
                        {
                            // All configuration values will be refreshed if the sentinel key changes.
                            refresh
                                .Register(sentinelKey, label, refreshAll: true)
                                .SetRefreshInterval(TimeSpan.FromSeconds(5));
                        })
                        .ConfigureKeyVault(kv => kv.SetCredential(creds));
                });

                // Registers IConfigurationRefresherProvider so the refresh middleware can
                // trigger configuration refresh per function invocation.
                builder.Services.AddAzureAppConfiguration();

                return builder;
            }
        }
    }
}
