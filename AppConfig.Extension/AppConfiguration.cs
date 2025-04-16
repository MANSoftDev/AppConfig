using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace AppConfig.Extension
{
    /// <summary>
    /// Extention class to facilitate the use of Azure App Configuration
    /// </summary>
    public static class AppConfiguration
    {
        /// <summary>
        /// Refresher for App Configuration
        /// </summary>
        public static IConfigurationRefresher? Refresher = null;

        /// <summary>
        /// Refresh the configuration
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public static async Task RefreshAsync()
        {
            if (Refresher != null)
            {
                var refreshed = await Refresher.TryRefreshAsync();
                if (refreshed)
                {
                    Console.WriteLine("Configuration refreshed successfully.");
                }
                else
                {
                    Console.WriteLine("Configuration refresh failed.");
                }
            }
        }
    }
}
