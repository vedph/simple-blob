using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using SimpleBlob.PgSql;
using System;
using System.Data.Common;
using System.Globalization;
using System.Threading.Tasks;

namespace SimpleBlobApi.Services
{
    /// <summary>
    /// Host seed extensions.
    /// </summary>
    public static class HostSeedExtensions
    {
        private static Task SeedAsync(IServiceProvider serviceProvider)
        {
            ApplicationDatabaseInitializer initializer =
                new ApplicationDatabaseInitializer(serviceProvider);

            return Policy.Handle<DbException>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60)
                }, (exception, timeSpan, _) =>
                {
                    ILogger logger = serviceProvider
                        .GetService<ILoggerFactory>()
                        .CreateLogger(typeof(HostSeedExtensions));

                    string message = "Unable to connect to DB" +
                        $" (sleep {timeSpan}): {exception.Message}";
                    Console.WriteLine(message);
                    logger.LogError(exception, message);
                }).Execute(async () =>
                {
                    await initializer.SeedAsync();
                });
        }

        /// <summary>
        /// Seeds the database.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>The received host, to allow concatenation.</returns>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public static async Task<IHost> SeedAsync(this IHost host)
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
                    //IHostEnvironment environment =
                    //    serviceProvider.GetService<IHostEnvironment>();

                    await SeedAsync(serviceProvider);
                    return host;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                    throw;
                }
            }
        }
    }
}
