using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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

namespace SimpleBlobApi
{
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // CORS (before MVC)
            ConfigureCorsServices(services);

            services.AddControllers();

            // camel-case JSON in response
            services.AddMvc()
                // https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-2.2&tabs=visual-studio#jsonnet-support
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // authentication
            // ConfigureAuthServices(services);

            services.AddSingleton(_ => Configuration);

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
            string maxSize = Configuration["Serilog:MaxMbSize"];
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
}
