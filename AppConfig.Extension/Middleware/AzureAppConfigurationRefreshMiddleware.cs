using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace AppConfig.Extension.Middleware
{
    /// <summary>
    /// Worker pipeline middleware that triggers Azure App Configuration refresh on each function
    /// invocation. This replaces the per-function <c>AppConfiguration.RefreshAsync()</c> call and the
    /// global mutable refresher state, resolving the refreshers from DI via
    /// <see cref="IConfigurationRefresherProvider"/> instead.
    /// </summary>
    /// <remarks>
    /// Refresh is fire-and-forget so it never blocks the invocation; the refresh interval configured on
    /// each refresher gates how often an actual network call is made. This mirrors the behavior of the
    /// ASP.NET Core <c>UseAzureAppConfiguration</c> middleware.
    /// </remarks>
    public sealed class AzureAppConfigurationRefreshMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IConfigurationRefresher[] _refreshers;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureAppConfigurationRefreshMiddleware"/> class.
        /// </summary>
        /// <param name="refresherProvider">Provides the configured App Configuration refreshers. Cannot be null.</param>
        public AzureAppConfigurationRefreshMiddleware(IConfigurationRefresherProvider refresherProvider)
        {
            ArgumentNullException.ThrowIfNull(refresherProvider);
            _refreshers = refresherProvider.Refreshers.ToArray();
        }

        /// <inheritdoc />
        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            foreach (var refresher in _refreshers)
            {
                _ = refresher.TryRefreshAsync();
            }

            await next(context);
        }
    }
}
