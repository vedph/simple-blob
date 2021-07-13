using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleBlob.PgSql;
using System;
using System.Globalization;

namespace SimpleBlobApi.Services
{
    /// <summary>
    /// Host seed extensions.
    /// </summary>
    public static class HostSeedExtensions
    {
        private static void EnsureDatabaseExists(string name,
            IConfiguration config,
            IHostEnvironment environment)
        {
            string cs = string.Format(
                CultureInfo.InvariantCulture,
                config.GetConnectionString("Default"),
                name);

            // check if DB exists
            Serilog.Log.Information($"Checking for database {name}...");

            PgSqlDbManager manager = new PgSqlDbManager(cs);

            if (!manager.Exists(name))
            {
                Serilog.Log.Information($"Creating database {name}...");
                PgSqlSimpleBlobStore store = new PgSqlSimpleBlobStore(cs);
                string sql = store.GetSchema();
                manager.CreateDatabase(name, sql, null);
                Serilog.Log.Information("Database created.");
            }
        }

        /// <summary>
        /// Seeds the database.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>The received host, to allow concatenation.</returns>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public static IHost Seed(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;
                ILogger logger = serviceProvider
                    .GetService<ILoggerFactory>()
                    .CreateLogger(typeof(HostSeedExtensions));

                try
                {
                    IConfiguration config =
                        serviceProvider.GetService<IConfiguration>();
                    IHostEnvironment environment =
                        serviceProvider.GetService<IHostEnvironment>();

                    EnsureDatabaseExists(
                        config.GetSection("DatabaseName").Get<string>(),
                        config, environment);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                    throw;
                }
            }
            return host;
        }
    }
}
