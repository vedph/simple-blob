using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleBlobApi.Auth;
using Fusi.DbManager.PgSql;
using System.IO;
using System.Reflection;
using System.Text;
using System.Globalization;
using SimpleBlob.PgSql;

namespace SimpleBlobApi.Services
{
    /// <summary>
    /// Application's user accounts database initializer.
    /// </summary>
    public sealed class ApplicationDatabaseInitializer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationSeededUserOptions[] _seededUsersOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDatabaseInitializer" />
        /// class.
        /// </summary>
        public ApplicationDatabaseInitializer(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetService<IConfiguration>();

            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<ApplicationDatabaseInitializer>();

            _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            _roleManager = serviceProvider.GetService<RoleManager<ApplicationRole>>();

            _seededUsersOptions = _configuration
                .GetSection("StockUsers")
                .Get<ApplicationSeededUserOptions[]>();
        }

        private void InitDatabase()
        {
            // check if DB exists
            string name = _configuration.GetValue<string>("DatabaseName");
            Serilog.Log.Information($"Checking for database {name}...");

            string csTemplate = _configuration.GetConnectionString("Default");
            PgSqlDbManager manager = new PgSqlDbManager(csTemplate);

            if (!manager.Exists(name))
            {
                Serilog.Log.Information($"Creating database {name}...");

                PgSqlSimpleBlobStore store = new PgSqlSimpleBlobStore(
                    string.Format(CultureInfo.InvariantCulture, csTemplate, name));
                string sql = store.GetSchema();

                manager.CreateDatabase(name, sql, LoadResourceText("Auth.pgsql"));
                Serilog.Log.Information("Database created.");
            }
        }

        private async Task SeedRoles()
        {
            foreach (ApplicationSeededUserOptions options in _seededUsersOptions
                .Where(o => o.Roles != null))
            {
                foreach (string roleName in options.Roles)
                {
                    // add role if not existing
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new ApplicationRole
                        {
                            Name = roleName
                        });
                    }
                }
            }
        }

        private async Task SeedUserAsync(ApplicationSeededUserOptions options)
        {
            ApplicationUser user =
                await _userManager.FindByNameAsync(options.UserName);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = options.UserName,
                    Email = options.Email,
                    // email is automatically confirmed for a stock user
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FirstName = options.FirstName,
                    LastName = options.LastName
                };
                IdentityResult result =
                    await _userManager.CreateAsync(user, options.Password);
                if (!result.Succeeded)
                {
                    _logger.LogError(result.ToString());
                    return;
                }
                user = await _userManager.FindByNameAsync(options.UserName);
            }
            // ensure that user is automatically confirmed
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            if (options.Roles != null)
            {
                foreach (string role in options.Roles)
                {
                    if (!await _userManager.IsInRoleAsync(user, role))
                        await _userManager.AddToRoleAsync(user, role);
                }
            }
        }

        private async Task SeedUsersWithRoles()
        {
            // roles
            await SeedRoles();

            // users
            if (_seededUsersOptions != null)
            {
                foreach (ApplicationSeededUserOptions options in _seededUsersOptions)
                    await SeedUserAsync(options);
            }
        }

        private static string LoadResourceText(string name)
        {
            using (StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    $"SimpleBlobApi.Assets.{name}"), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Seeds the database.
        /// </summary>
        public async Task SeedAsync()
        {
            InitDatabase();
            await SeedUsersWithRoles();
        }
    }
}
