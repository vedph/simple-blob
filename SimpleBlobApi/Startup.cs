using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SimpleBlob.PgSql;
using SimpleBlob.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Logging;
using SimpleBlobApi.Services;
using System.Globalization;
using MessagingApi;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Fusi.Api.Auth.Services;
using SimpleBlobApi.Models;

namespace SimpleBlobApi;

/// <summary>
/// Startup class.
/// </summary>
public sealed class Startup
{
    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the host environment.
    /// </summary>
    public IHostEnvironment HostEnvironment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The environment.</param>
    public Startup(IConfiguration configuration, IHostEnvironment environment)
    {
        Configuration = configuration;
        HostEnvironment = environment;
    }

    private void ConfigureOptionsServices(IServiceCollection services)
    {
        // configuration sections
        // https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-asp-net-core/
        services.Configure<MessagingOptions>(Configuration.GetSection("Messaging"));
        services.Configure<DotNetMailerOptions>(Configuration.GetSection("Mailer"));

        // explicitly register the settings object by delegating to the IOptions object
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<MessagingOptions>>().Value);
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<DotNetMailerOptions>>().Value);
    }

    private void ConfigureCorsServices(IServiceCollection services)
    {
        string[] origins = new[] { "http://localhost:4200" };

        IConfigurationSection section = Configuration.GetSection("AllowedOrigins");
        if (section.Exists())
        {
            origins = section.AsEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => p.Value).ToArray();
        }

        services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyMethod()
                .AllowAnyHeader()
                // https://github.com/aspnet/SignalR/issues/2110 for AllowCredentials
                .AllowCredentials()
                .WithOrigins(origins);
        }));
    }

    private void ConfigureAuthServices(IServiceCollection services)
    {
        // identity
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                string.Format(CultureInfo.InvariantCulture,
                Configuration.GetConnectionString("Default"),
                Configuration.GetValue<string>("DatabaseName")));
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // authentication service
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(options =>
           {
               // NOTE: remember to set the values in configuration:
               // Jwt:SecureKey, Jwt:Audience, Jwt:Issuer
               IConfigurationSection jwtSection = Configuration.GetSection("Jwt");
               string key = jwtSection["SecureKey"];
               if (string.IsNullOrEmpty(key))
                   throw new InvalidOperationException("Required JWT SecureKey not found");

               options.SaveToken = true;
               options.RequireHttpsMetadata = false;
               options.TokenValidationParameters = new TokenValidationParameters()
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidAudience = jwtSection["Audience"],
                   ValidIssuer = jwtSection["Issuer"],
                   IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(key))
               };

               // support refresh
               // https://stackoverflow.com/questions/55150099/jwt-token-expiration-time-failing-net-core
               options.Events = new JwtBearerEvents
               {
                   OnAuthenticationFailed = context =>
                   {
                       if (context.Exception.GetType() ==
                            typeof(SecurityTokenExpiredException))
                       {
                           context.Response.Headers.Add("Token-Expired", "true");
                       }
                       return Task.CompletedTask;
                   }
               };
           });
#if DEBUG
        // use to show more information when troubleshooting JWT issues
        IdentityModelEventSource.ShowPII = true;
#endif
    }

    private static void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "API",
                Description = "BLOB API Services"
            });
            c.DescribeAllParametersInCamelCase();

            // include XML comments
            // (remember to check the build XML comments in the prj props)
            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);

            // JWT
            // https://stackoverflow.com/questions/58179180/jwt-authentication-and-swagger-with-net-core-3-0
            // (cf. https://ppolyzos.com/2017/10/30/add-jwt-bearer-authorization-to-swagger-and-asp-net-core/)
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please insert JWT with Bearer into field",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
            });
        });
    }

    private static void ConfigureMessagingServices(IServiceCollection services)
    {
        services.AddScoped<IMailerService, DotNetMailerService>();
        services.AddScoped<IMessageBuilderService,
            FileMessageBuilderService>();
    }

    /// <summary>
    /// Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // CORS (before MVC)
        ConfigureCorsServices(services);
        // options
        ConfigureOptionsServices(services);

        services.AddControllers();

        // camel-case JSON in response
        services.AddMvc()
            // https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-2.2&tabs=visual-studio#jsonnet-support
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;
            });

        // configuration
        services.AddSingleton(_ => Configuration);

        // authentication
        ConfigureAuthServices(services);

        // messaging
        ConfigureMessagingServices(services);

        // user repository service
        services.AddScoped<IUserRepository<ApplicationUser>,
            UserRepository<ApplicationUser, ApplicationRole>>();

        // BLOB services
        services.AddScoped<ISimpleBlobStore>(_ =>
        {
            return new PgSqlSimpleBlobStore(
                string.Format(
                    Configuration.GetConnectionString("Default"),
                    Configuration.GetValue<string>("DatabaseName")));
        });

        // swagger
        ConfigureSwaggerServices(services);

        // serilog
        // https://github.com/RehanSaeed/Serilog.Exceptions
        services.AddSingleton<ILogger>(
            _ => new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .CreateLogger());
    }

    /// <summary>
    /// Configures the specified application.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.2#configure-a-reverse-proxy-server
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio
            app.UseExceptionHandler("/Error");
            if (Configuration.GetValue<bool>("Server:UseHSTS"))
            {
                Console.WriteLine("Using HSTS");
                app.UseHsts();
            }
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        // CORS
        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            string url = Configuration.GetValue<string>("Swagger:Endpoint");
            if (string.IsNullOrEmpty(url)) url = "v1/swagger.json";
            options.SwaggerEndpoint(url, "V1 Docs");
        });
    }
}
