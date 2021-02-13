using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Hosting;
using PikaCore.Areas.Api.v1.Services;
using PikaCore.Areas.Core.Controllers.App;
using PikaCore.Areas.Core.Controllers.Hubs;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Services;
using PikaCore.Infrastructure.Data;
using PikaCore.Infrastructure.Security;
using PikaCore.Infrastructure.Services;
using PikaCore.Properties;
using Quartz;
using Serilog;
using StackExchange.Redis;
using TanvirArjel.CustomValidation.AspNetCore.Extensions;
using WebSocketOptions = Microsoft.AspNetCore.Builder.WebSocketOptions;

namespace PikaCore
{
    public class Startup
    {
        private static readonly string OsName = Constants.OsName;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Constants.Instance = Configuration.GetSection("Name:Instance").Value ?? "Default";
            var path = Path.Combine(Configuration.GetSection("Logging").GetSection("LogDirs")[OsName + "-log"],
                $"pika_core_{Constants.Instance}.{DateTime.Today.Day}-{DateTime.Today.Month}-{DateTime.Today.Year}.log");
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(path,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<StorageIndexContext>(options => 
                options.UseNpgsql(Configuration.GetConnectionString("StorageConnection")));

            services.AddDbContext<SystemContext>(options => 
                options.UseNpgsql(Configuration.GetConnectionString("StorageConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
                {
                    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                        int.Parse(Configuration
                            .GetSection("Policies")
                            .GetSection("LoginPolicy")["DefaultLockout"])
                        );
                    opt.Lockout.MaxFailedAccessAttempts = int.Parse(Configuration
                        .GetSection("Policies")
                        .GetSection("LoginPolicy")["MaxFailedAttempts"]);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            services.AddDistributedMemoryCache();
            services.AddStackExchangeRedisCache(a => {
                a.InstanceName = Configuration.GetSection("Redis")["InstanceName"];
                a.Configuration = Configuration.GetConnectionString("RedisConnection");
            });
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("RedisConnection"));
            
            services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 4096;
                options.UseCaseSensitivePaths = true;
            });
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddDataProtection()
                    .ProtectKeysWithDpapi();
            }
            else if(redis.IsConnected)
            {
                var key = string.Concat("CoreKeys-", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, key)
                    .ProtectKeysWithCertificate(
                        new X509Certificate2(Configuration["Security:CertificatePath"], 
                        Configuration["Security:Passphrase"]))
                    .SetApplicationName("ShrCkApp");
            }else
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/srv/fms/keys"))
                    .ProtectKeysWithCertificate(
                        new X509Certificate2(Configuration["Security:CertificatePath"], 
                        Configuration["Security:Passphrase"]))
                    .SetApplicationName("ShrCkApp");
            }
            services.AddAuthentication()
                .AddGoogle(googleOpts =>
                {
                    googleOpts.ClientId = Configuration["Authentication:Google:ClientId"];
                    googleOpts.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                })
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ApplicationId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:Password"];
                })
                .AddGitHub(githubOptions =>
                {
                    githubOptions.ClientId = Configuration["Authentication:GitHub:ClientId"];
                    githubOptions.ClientSecret = Configuration["Authentication:GitHub:ClientSecret"];
                    githubOptions.CallbackPath = "/signin-github";
                })
                .AddDiscord(discordOptions =>
                {
                    discordOptions.ClientId = Configuration["Authentication:Discord:ClientId"];
                    discordOptions.ClientSecret = Configuration["Authentication:Discord:ClientSecret"];
                });
            
            services.AddAspNetCoreCustomValidation();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IUrlGenerator, HashUrlGeneratorService>();
            services.AddTransient<IStreamingService, StreamingService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddSingleton<ISchedulerService, SchedulerService>();
            services.AddSingleton<UniqueCode>();
            services.AddSingleton<IdDataProtection>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddTransient<ISystemService, SystemService>();
            services.AddTransient<IStatusService, StatusService>();
            services.AddTransient<IJobService, JobService>();
            services.AddTransient<IStaticContentService, StaticContentService>();
            services.AddTransient<IDataExportService, DataExportService>();
            
           services.AddLocalization(options => options.ResourcesPath = "Resources"); 
             
               services.Configure<RequestLocalizationOptions>(options =>  
               {  
                   var supportedCultures = new[]  
                   {  
                           new CultureInfo("en"),  
                           new CultureInfo("pl")  
                   };  
             
                   options.DefaultRequestCulture = new RequestCulture(culture: "en", uiCulture: "en");  
                   options.SupportedCultures = supportedCultures;  
                   options.SupportedUICultures = supportedCultures;  
               });
               
            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
            
            
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 268435456; //256MB
            });
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });
            
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
                options.SlidingExpiration = true;
                options.LoginPath = "/Identity/Account/Login";
                options.Cookie.Name = ".AspNet.ShrCk";
                options.LogoutPath = "/Identity/Account/Logout";
            });
            
    	    services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithMethods("POST", "GET")
                    .WithOrigins("https://dev-core.lukas-bownik.net", 
                        "https://core.lukas-bownik.net", 
                        "https://me.lukas-bownik.net", 
                        "https://www.lukas-bownik.net",
                        "http://localhost:5000")
                    .AllowAnyHeader();
            }));
            services.AddHealthChecks();

            services.AddSignalR(hubOptions =>
                                    {
                                        hubOptions.EnableDetailedErrors = true;
                                        hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
                                    })
                .AddStackExchangeRedis(o =>
            {
                o.Configuration.ClientName = "PikaCore";
                o.Configuration.ChannelPrefix = "PikaCoreHub";
            });
            
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            IFileProvider physicalProvider = new PhysicalFileProvider(Configuration.GetSection("Paths")[OsName + "-root"]);
            services.AddSingleton(physicalProvider);

            Constants.UploadDirectory = Configuration.GetSection("Paths")["upload-dir-" + OsName];
            Constants.UploadTmp = Configuration.GetSection("Paths")["upload-dir-tmp"];
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressInferBindingSourcesForParameters = true;
                    options.SuppressModelStateInvalidFilter = true;
                    options.SuppressMapClientErrors = true;
                });
            services.AddRazorPages()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeAreaFolder("Core", "/Admin");
                });
            
            services.AddResponseCaching(opt =>
            {
                opt.UseCaseSensitivePaths = true;
                opt.SizeLimit = 819200;
            });

            services.AddResponseCompression(opt =>
            {
                opt.EnableForHttps = true;
                opt.MimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            });
            
            
            services.AddQuartz();
            services.AddQuartzServer(options =>
            {
                options.WaitForJobsToComplete = true;
            });
            
            services.AddMvc()
                .AddMvcOptions(options =>
                {
                    options.AllowEmptyInputInBodyModelBinding = true;
                    options.CacheProfiles.Add("Default",
                    new CacheProfile()
                    {
                        Duration = 360000,
                        Location = ResponseCacheLocation.Client,
                        NoStore = false
                    });
                    options.MaxModelValidationErrors = 50;
                })
	        .SetCompatibilityVersion(CompatibilityVersion.Latest);

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
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
                        
            app.UseRequestLocalization(localizationOptions);

            lifetime.ApplicationStopping.Register(OnShutdown);

            app.UseStatusCodePages();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Core/Home/Error");
                app.UseHsts();
                app.UseCertificateForwarding();
            }
            Constants.RootPath = Configuration.GetSection("Paths")["storage"];
            Constants.FileSystemRoot = Configuration.GetSection("Paths")[OsName + "-root"];
            Constants.Tmp = Configuration.GetSection("Paths")[OsName + "-tmp"];
            Constants.MaxUploadSize = long.Parse(Configuration.GetSection("Storage")["maxUploadSize"]);
            
            // app.UseStatusCodePagesWithRedirects("/Core/Home/ErrorByCode/{0}");
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
                        ctx.Context.Response.Headers.Append("Pragma", "no-cache, no-store");
                    }
                }
            );

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(Configuration.GetSection("Storage")["staticFiles"]),
                RequestPath = "/Static",
                EnableDirectoryBrowsing = false
            });
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
            };
            webSocketOptions.AllowedOrigins.Add("https://dev-core.lukas-bownik.net");
            webSocketOptions.AllowedOrigins.Add("https://core.lukas-bownik.net");
            app.UseWebSockets(webSocketOptions);
            app.UseRouting();
	        app.UseCors("CorsPolicy");
            app.UseResponseCaching();
            app.UseAuthentication();
            

            app.UseResponseCompression();
            app.UseAuthorization();
            
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
                        HttpTransportType.WebSockets |
                        HttpTransportType.ServerSentEvents;
                });
                endpoints.MapHub<FileOperationHub>("/hubs/files", options =>
                {
                    options.Transports =
                        HttpTransportType.WebSockets |
                        HttpTransportType.ServerSentEvents;
                });
                endpoints.MapHub<MediaHub>("/hubs/media", options =>
                {
                    options.Transports =
                        HttpTransportType.ServerSentEvents |
                        HttpTransportType.WebSockets;
                });
                endpoints.MapHealthChecks("/core/health")
                    .RequireCors("CorsPolicy")
                    .RequireHost("localhost"); 
            });
            
            CreateRoles(serviceProvider).Wait();
        }

        private async Task CreateRoles(IServiceProvider serviceProvider)
        {
            //adding custom roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            string[] roleNames = { "Admin", "FileManagerUser", "User" };

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            //creating a super user who could maintain the web app
            var poweruser = new ApplicationUser
            {
                UserName = Configuration.GetSection("UserSettings")["UserEmail"],
                Email = Configuration.GetSection("UserSettings")["UserEmail"]
            };

            var userPassword = Configuration.GetSection("UserSettings")["UserPassword"];
            var user = await userManager.FindByEmailAsync(Configuration.GetSection("UserSettings")["UserEmail"]);

            //Debug.WriteLine(_user.Email);
            if (user == null)
            {
                var createPowerUser = await userManager.CreateAsync(poweruser, userPassword);
                if (createPowerUser.Succeeded)
                {
                    //here we tie the new user to the "Admin" role 
                    await userManager.AddToRoleAsync(poweruser, "Admin");
                }
            }
        }
        
        #region LifetimeHelpers
        
        private static void OnShutdown()
        {
            Log.Information("System is stopping... Good bye.");
        }
        #endregion
    }
}
