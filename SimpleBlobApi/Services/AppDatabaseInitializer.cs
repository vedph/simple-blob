using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Fusi.DbManager.PgSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SimpleBlob.PgSql;
using System;
using System.IO;
using System.Reflection;

namespace SimpleBlobApi.Services;

/// <summary>
/// Application's user accounts database initializer.
/// </summary>
public sealed class AppDatabaseInitializer :
    AuthDatabaseInitializer<NamedUser, IdentityRole, NamedSeededUserOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDatabaseInitializer"/>
    /// class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public AppDatabaseInitializer(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// Initializes the user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="options">The options.</param>
    protected override void InitUser(NamedUser user,
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
        string name = Configuration.GetValue<string>("DatabaseNames:Auth")!;
        Serilog.Log.Information("Checking for auth database {Name}...", name);

        string csTemplate = Configuration.GetConnectionString("Auth")!;
        PgSqlDbManager manager = new(csTemplate);

        if (!manager.Exists(name))
        {
            Serilog.Log.Information("Creating auth database {Name}...", name);

            using (StreamReader reader = new(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "SimpleBlobApi.Assets.Auth.pgsql")!))
            {
                string sql = reader.ReadToEnd();
                manager.CreateDatabase(name, sql, PgSqlSimpleBlobStore.GetDDL());
            }

            Serilog.Log.Information("Auth database created.");
        }
    }
}
