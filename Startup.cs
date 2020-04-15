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
using Microsoft.Extensions.Logging;
using PikaCore.Controllers.Hubs;
using PikaCore.Services;
using PikaCore.Services.Helpers;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Hosting;
using PikaCore.Controllers.Api.Hubs;
using PikaCore.Controllers.App;
using PikaCore.Data;
using PikaCore.Extensions;
using PikaCore.Models;
using PikaCore.Security;

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

            services.AddIdentity<ApplicationUser, IdentityRole>()
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

            var opts = new Pomelo.Logging.FileLogger.FileLoggerOptions()
            {
                FileName = $"pika_core_{DateTime.Today.ToShortDateString()}.log",
                MaxSize = Constants.MaxLogFileSize,
                OutputFolder = Configuration.GetSection("Logging").GetSection("LogDirs")[OsName + "-log"],
            };
            Console.WriteLine(opts.OutputFolder);
            services.AddSingleton<ILoggerProvider>(loggerProvider => new Pomelo.Logging.FileLogger.FileLoggerProvider(opts));

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
                app.UseExceptionHandler("/Home/Error");
            }

            Constants.RootPath = Configuration.GetSection("Paths")["storage"];
            Constants.FileSystemRoot = Configuration.GetSection("Paths")[OsName + "-root"];
            Constants.Tmp = Configuration.GetSection("Paths")[OsName + "-tmp"];
            Constants.MaxUploadSize = long.Parse(Configuration.GetSection("Storage")["maxUploadSize"]);

            app.UseStaticFiles();
            app.UseFileServer();
            app.UseStatusCodePagesWithRedirects("/Home/ErrorByCode/{0}");
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
                    pattern: "{controller=Home}/{action=Index}/{id?}");
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
                        HttpTransportType.LongPolling |
                        HttpTransportType.ServerSentEvents;
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
