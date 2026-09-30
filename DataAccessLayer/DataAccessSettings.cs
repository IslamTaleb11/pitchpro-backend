
using Microsoft.Extensions.Configuration;

namespace DataAccessLayer
{
    internal class DataAccessSettings
    {
        public static readonly string connectionString;

        static DataAccessSettings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Database connection string not found. Set 'ConnectionStrings__DefaultConnection' in your environment or 'DefaultConnection' in appsettings.json.");
        }
    }
}
