using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager.PgSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SimpleBlob.PgSql;
using SimpleBlobApi.Models;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleBlobApi.Services;

/// <summary>
/// Application database initializer.
/// </summary>
public sealed class ApplicationDatabaseInitializer :
    AuthDatabaseInitializer<ApplicationUser, ApplicationRole, NamedSeededUserOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDatabaseInitializer"/>
    /// class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public ApplicationDatabaseInitializer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
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
    /// Initializes the user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="options">The options.</param>
    protected override void InitUser(ApplicationUser user,
        NamedSeededUserOptions options)
    {
        base.InitUser(user, options);

        user.FirstName = options.FirstName;
        user.LastName = options.LastName;
    }

    /// <summary>
    /// Initializes the database.
    /// </summary>
    protected override void InitDatabase()
    {
        // check if DB exists
        string name = Configuration.GetValue<string>("DatabaseName");
        Serilog.Log.Information($"Checking for database {name}...");

        string csTemplate = Configuration.GetConnectionString("Default");
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
}
