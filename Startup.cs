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
using PikaCore.Services;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PikaCore.Areas.Core.Controllers.App;
using PikaCore.Areas.Core.Controllers.Hubs;
using PikaCore.Areas.Core.Data;
using PikaCore.Areas.Core.Extensions;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Services;
using PikaCore.Data;
using PikaCore.Models;
using PikaCore.Properties;
using PikaCore.Security;
using FileLoggerProvider = Germes.AspNetCore.FileLogger.FileLoggerProvider;

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
            services.AddMemoryCache();
            services.AddStackExchangeRedisCache(a => { 
                a.InstanceName = Configuration.GetSection("Redis")["InstanceName"];
                a.Configuration = Configuration.GetConnectionString("RedisConnection");
            });
            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddDbContext<StorageIndexContext>(options => 
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

            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddScoped<IArchiveService, ArchiveService>();
            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IUrlGenerator, HashUrlGeneratorService>();
            services.AddTransient<IStreamingService, StreamingService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddSingleton<ISchedulerService, SchedulerService>();
            services.AddSingleton<UniqueCode>();
            services.AddSingleton<IdDataProtection>();
            
            var path = Path.Combine(Configuration.GetSection("Logging").GetSection("LogDirs")[OsName + "-log"],
                $"pika_core_{DateTime.Today.Day}-{DateTime.Today.Month}-{DateTime.Today.Year}.log");
            Console.WriteLine(Resources.Startup_ConfigureServices_Logger_output___0_, path);
            
            var provider = new FileLoggerProvider(path, LogLevel.Debug);
            services.AddSingleton(provider);
            
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
                options.SlidingExpiration = true;
                options.LoginPath = "/Account/Login";
            });
            
    	    services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithMethods("GET", "POST")
                    .WithOrigins("dev-core.lukas-bownik.net", "core.lukas-bownik.net", "me.lukas-bownik.net", "localhost:5000")
                    .AllowAnyHeader();
            }));
            
            services.AddSignalR();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            services.AddSingleton<IFileLoggerService, FileLoggerService>();

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
                    options.ClientErrorMapping[404].Link = "/Api/v1/notfoundhandler";
                });
            
            services.AddMvc()
	        .AddRazorPagesOptions(options =>
            {
                options.Conventions
                    .AddPageApplicationModelConvention("/StreamedSingleFileUploadPhysical",
                        model =>
                        {
                            model.Filters.Add(
                                new GenerateAntiforgeryTokenCookieAttribute());
                            model.Filters.Add(
                                new DisableFormValueModelBindingAttribute());
                        });
            })
            .AddMvcOptions(options =>
            {
                options.MaxModelValidationErrors = 50;
                options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
                    _ => "The field is required.");
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
                              IServiceProvider serviceProvider
                             )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Core/Home/Error");
            }

            Constants.RootPath = Configuration.GetSection("Paths")["storage"];
            Constants.FileSystemRoot = Configuration.GetSection("Paths")[OsName + "-root"];
            Constants.Tmp = Configuration.GetSection("Paths")[OsName + "-tmp"];
            Constants.MaxUploadSize = long.Parse(Configuration.GetSection("Storage")["maxUploadSize"]);

            app.UseStaticFiles();
            app.UseFileServer();
            app.UseStatusCodePagesWithRedirects("/Core/Home/ErrorByCode/{0}");
            app.UseSession();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseWebSockets();
            app.UseResponseCaching();
	        app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseRouting();
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
                        HttpTransportType.LongPolling |
                        HttpTransportType.ServerSentEvents;
                });
                endpoints.MapHub<FileOperationHub>("/hubs/files", options =>
                {
                    options.Transports =
                        HttpTransportType.LongPolling |
                        HttpTransportType.ServerSentEvents;
                });
                endpoints.MapHub<MediaHub>("/hubs/media", options =>
                {
                    options.Transports =
                        HttpTransportType.ServerSentEvents |
                        HttpTransportType.WebSockets;
                });
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
    }
}
