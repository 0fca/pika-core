using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Hangfire;
using Hangfire.Dashboard.Resources;
using Hangfire.Redis;
using Marten;
using Marten.Events.Projections;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenIddict.Client;
using OpenIddict.Validation.AspNetCore;
using Pika.Domain.Storage.Entity.Projection;
using Pika.Domain.Storage.Repository;
using PikaCore.Areas.Api.v1.Services;
using PikaCore.Areas.Core.Callables;
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Controllers.Hubs;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Queries;
using PikaCore.Areas.Core.Repository;
using PikaCore.Areas.Core.Services;
using PikaCore.Areas.Identity.Extensions;
using PikaCore.Areas.Identity.Filters;
using PikaCore.Infrastructure.Adapters;
using PikaCore.Infrastructure.Adapters.Console;
using PikaCore.Infrastructure.Adapters.Console.Commands;
using PikaCore.Infrastructure.Adapters.Console.Queries;
using PikaCore.Infrastructure.Security;
using PikaCore.Infrastructure.Services;
using Serilog;
using StackExchange.Redis;
using TanvirArjel.CustomValidation.AspNetCore.Extensions;
using Weasel.Core;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace PikaCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Warning()
                .WriteTo.Console()
                .CreateLogger();
            services.AddLogging();
            services.AddMarten(c =>
                {
                    c.Connection(Configuration.GetConnectionString("DefaultConnection"));
                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Equals("Development"))
                    {
                        c.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
                    }

                    c.Projections.Add<BucketsProjection>(ProjectionLifecycle.Inline);
                    c.Projections.Add<CategoriesProjection>(ProjectionLifecycle.Inline);
                    c.Projections.Add<CommandsProjection>(ProjectionLifecycle.Inline);
                })
                .UseLightweightSessions();
            services.AddDbContext<StorageIndexContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddStackExchangeRedisCache(a =>
            {
                a.InstanceName = Configuration.GetSection("Redis")["InstanceName"];
                a.ConfigurationOptions = new ConfigurationOptions
                {
                    DefaultDatabase = int.Parse(Configuration.GetSection("Redis")["RedisDb"] ?? "0"),
                    EndPoints = { Configuration.GetConnectionString("RedisConnection") }
                };
            });
            var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { Configuration.GetConnectionString("RedisConnection") }
            });
            services.AddMemoryCache();

            services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 4096;
                options.UseCaseSensitivePaths = true;
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\PikaCloud\\PikaCore\\SecurityKeyRing", true);
                services.AddDataProtection()
                    .PersistKeysToRegistry(regKey)
                    .ProtectKeysWithDpapi();
            }
            else if (redis.IsConnected)
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var key = string.Concat("CoreKeys-", env);
                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, key)
                    .ProtectKeysWithCertificate(
                        new X509Certificate2(Configuration["Security:CertificatePath"],
                            Configuration["Security:Passphrase"]))
                    .SetApplicationName("ShrCkApp");
            }
            else
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/srv/fms/keys"))
                    .ProtectKeysWithCertificate(
                        new X509Certificate2(Configuration["Security:CertificatePath"],
                            Configuration["Security:Passphrase"]))
                    .SetApplicationName("ShrCkApp");
            }

            services.AddOpenIddict()
                .AddClient(o =>
                {
                    o.AllowPasswordFlow();
                    o.AllowRefreshTokenFlow();
                    o.UseSystemNetHttp()
                        .SetProductInformation(typeof(Program).Assembly);
                    o.AddRegistration(new OpenIddictClientRegistration
                    {
                        ClientId = Configuration.GetSection("Auth")["ClientId"],
                        ClientSecret = Configuration.GetSection("Auth")["ClientSecret"],
                        Issuer = new Uri(Configuration.GetSection("Auth")["Authority"], UriKind.Absolute)
                    });
                })
                .AddValidation(o =>
                {
                    o.SetIssuer(Configuration.GetSection("Auth")["Authority"]);
                    o.UseDataProtection();
                    o.UseIntrospection()
                        .SetClientId(Configuration.GetSection("Auth")["ClientId"])
                        .SetClientSecret(Configuration.GetSection("Auth")["ClientSecret"]);
                    o.UseSystemNetHttp();
                    o.UseAspNetCore();
                });

            services.AddAuthentication(o =>
            {
                o.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });
            services.AddAuthorization();
            services.AddHangfire(o =>
            {
                o.UseSerializerSettings(new JsonSerializerSettings()
                {
                    MaxDepth = 128,
                    MissingMemberHandling = MissingMemberHandling.Error,
                    ObjectCreationHandling = ObjectCreationHandling.Auto
                });
                if (!string.IsNullOrEmpty(Configuration.GetConnectionString("RedisConnection")))
                {
                    o.UseRedisStorage(Configuration.GetConnectionString("RedisConnection"),
                        new RedisStorageOptions()
                        {
                            Db = int.Parse(Configuration.GetSection("Hangfire")["RedisDb"] ?? "0")
                        });
                }
            });
            services.AddHangfireServer();
            services.AddAspNetCoreCustomValidation();
            services.AddTransient<IHashGenerator, HashGeneratorService>();
            services.AddSingleton<UniqueCode>();
            services.AddSingleton<IdDataProtection>();
            services.AddTransient<CloudConsoleAdapter>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<AggregateRepository>();
            services.AddTransient<ISystemService, SystemService>();
            services.AddTransient<IStatusService, StatusService>();
            services.AddTransient<IDataExportService, DataExportService>();
            services.AddScoped<IOidcService, OidcService>();
            services.AddSingleton<IMinioService, MinioService>();
            services.AddTransient<CategoryRepository>();
            services.AddTransient<BucketRepository>();
            services.AddTransient<CommandRepository>();
            services.AddTransient<IStorage, MinioStorage>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("pl")
                };

                options.DefaultRequestCulture = new RequestCulture(
                    culture: Configuration.GetSection("UI")["DefaultCulture"],
                    uiCulture: Configuration.GetSection("UI")["DefaultCulture"]
                );
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 268435456; //256MB
            });

            services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithMethods("POST", "GET")
                    .WithOrigins("https://core.lukas-bownik.net",
                        "https://lukas-bownik.net",
                        "https://me.lukas-bownik.net",
                        "https://www.lukas-bownik.net",
                        "http://core.cloud.localhost:5000",
                        "https://core.cloud.localhost:5001",
                        "http://192.168.56.1:8080")
                    .AllowAnyHeader();
            }));

            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(5);
            });
                /*.AddStackExchangeRedis(o =>
                {
                    o.Configuration.ClientName = "PikaCore";
                    o.Configuration.ChannelPrefix = "PikaCoreHub";
                    o.Configuration.EndPoints.Add(Configuration.GetConnectionString("RedisConnection"));
                });*/

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.AddControllers()
                .AddRazorRuntimeCompilation()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressInferBindingSourcesForParameters = true;
                    options.SuppressModelStateInvalidFilter = true;
                    options.SuppressMapClientErrors = true;
                });
            services.AddRazorPages()
                .AddRazorPagesOptions(options => { options.Conventions.AuthorizeAreaFolder("Admin", "/Index"); });
            
            services.AddHealthChecks();

            services.AddResponseCompression(opt =>
            {
                opt.EnableForHttps = true;
                opt.MimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            });
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization()
                .AddMvcOptions(options =>
                {
                    options.AllowEmptyInputInBodyModelBinding = true;
                    options.CacheProfiles.Add("Default",
                        new CacheProfile()
                        {
                            Duration = 360000,
                            Location = ResponseCacheLocation.Client,
                            NoStore = true
                        });
                    options.MaxModelValidationErrors = 50;
                });


            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime lifetime
        )
        {
            var supportedCultures = new[] { "en", "pl" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(Configuration.GetSection("UI")["DefaultCulture"] ?? "pl")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);
            lifetime.ApplicationStopping.Register(OnShutdown);
            lifetime.ApplicationStarted.Register(OnStartup);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePagesWithRedirects("/Core/Status/{0}");
            }
            else
            {
                app.UseExceptionHandler("/Core/Error");
                app.UseStatusCodePagesWithRedirects("/Core/Status/{0}");
                app.UseCertificateForwarding();
            }

            app.UseSession();
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            var cacheMaxAge = Configuration.GetSection("Storage")["cacheMaxAge"];
            app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Response.Headers.Append(
                            "Cache-Control", $"public, max-age={cacheMaxAge}");
                    }
                }
            );
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(1200),
            };
            //webSocketOptions.AllowedOrigins.Add("https://core.lukas-bownik.net");
            app.UseWebSockets(webSocketOptions);
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseResponseCaching();
            app.UseOiddictAuthenticationCookieSupport();
            app.UseAuthentication();
            app.UseResponseCompression();
            app.UseMapJwtClaimsToIdentity();
            app.UseEnsureJwtBearerValid();
            app.UseAuthorization();
            app.UseMinioBucketAccessAuthorization();
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "fallback",
                    pattern: "{area=Core}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapHub<StatusHub>("/hubs/status", options =>
                {
                    options.Transports =
                        HttpTransportType.LongPolling;
                });
                endpoints.MapHub<FileOperationHub>("/hubs/storage", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
                });
                endpoints.MapHub<MediaHub>("/hubs/media", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets;
                });
                endpoints.MapHealthChecks("/Health");
            });
            if (env.IsDevelopment())
            {
                this.CreateBuckets(serviceProvider);
                this.CreateDefaultCommands(serviceProvider);
            }

            if (bool.Parse(Configuration.GetSection("Workers")["RunWorkers"] ?? "true"))
            {
                this.RegisterRecurringCategoryJobs(serviceProvider);
            }
            else
            {
                Log.Warning("Workers won't be running, because disabled by user!");
            }
        }

        private void RegisterRecurringCategoryJobs(IServiceProvider serviceProvider)
        {
            var cache = serviceProvider.GetService<IDistributedCache>();
            var mediator = serviceProvider.GetService<IMediator>();
            var mapper = serviceProvider.GetService<IMapper>();
            var clientService = serviceProvider.GetService<IMinioService>();
            var refreshCallable = new RefreshCategoriesCallable(cache,
                mediator,
                clientService,
                mapper);
            RecurringJob.AddOrUpdate("UpdateCategories", () =>
                    refreshCallable.Execute(null),
                Configuration
                    .GetSection("Storage")
                    .GetSection("Workers")["CategoriesRefreshWorkerCron"]
            );
            var updateTagsCallables = new GenerateCategoriesTagsCallable(mediator, clientService, cache);
            RecurringJob.AddOrUpdate("UpdateCategoriesTags",
                () => updateTagsCallables.Execute(null),
                Configuration
                    .GetSection("Storage")
                    .GetSection("Workers")["CategoriesTagsRefreshWorkerCron"]);
        }

        private void CreateBuckets(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetService<IMinioService>();
            var mediator = serviceProvider.GetService<IMediator>();
            var buckets = client.GetBuckets().Result;
            var savedBuckets = mediator.Send(new GetAllBucketsQuery()).Result;
            buckets.ToList().ForEach(b =>
            {
                if (!savedBuckets.Any(sb => sb.Name.Equals(b.Name)))
                {
                    mediator.Send(new CreateBucketCommand
                    {
                        Name = b.Name,
                        RoleClaims = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                            Configuration.GetSection($"Minio:Buckets:{b.Name}").Value
                        )
                    });
                }
            });
        }

        private void CreateDefaultCommands(IServiceProvider serviceProvider)
        {
            var mediator = serviceProvider.GetService<IMediator>();
            var defaultCommands = new List<Tuple<string, HashSet<string>, string>>
            {
                new(".SYSSTA", new HashSet<string>(){"TCP"}, ""),
                new(".USRNFO", new HashSet<string>(){"ALL"}, "ofca"),
                new(".DIR", new HashSet<string>(){ "S1", "0" }, ""),
                new(".S.CRT", new HashSet<string>(), ""),
            };
            foreach (var (name, headers, body) in defaultCommands)
            {
                var exists = mediator.Send(new AnyExistsByNameQuery(name)).Result;
                if (!exists)
                {
                    mediator.Send(new CreateConsoleCmdCommand(name, headers, body));
                }
            }
        }

        #region LifetimeHelpers

        private static void OnStartup()
        {
            var currentVersion = (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException())
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            Console.WriteLine($"PikaCore v.{currentVersion} is booting... Hellorld!");
        }

        private static void OnShutdown()
        {
            Console.WriteLine("PikaCore is shutting down... Good bye.");
        }

        #endregion
    }
}