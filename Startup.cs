using FMS2.Controllers;
using FMS2.Controllers.Api.Hubs;
using FMS2.Data;
using FMS2.Models;
using FMS2.Providers;
using FMS2.Services;
using FMS2.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PikaCore.Controllers.Api.Hubs;
using PikaCore.Services;
using PikaCore.Services.Helpers;
using System;
using System.Threading.Tasks;

namespace FMS2
{
    public class Startup
    {
        private static readonly string OsName = Controllers.Constants.OsName;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {

            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //var key = Encoding.ASCII.GetBytes(Configuration.GetSection("TokenSettings")["Secret"]);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<StorageIndexContext>(options => options.UseMySql(Configuration.GetConnectionString("StorageConnection")));

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
            services.AddScoped<IZipper, ArchiveService>();
            services.AddSingleton<ImageCache>();
            services.AddTransient<IFileService, FileService>();
            services.AddTransient<IGenerator, HashGeneratorService>();
            services.AddTransient<IStreamingService, StreamingService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddSingleton<ISchedulerService, SchedulerService>();

            var option = new FileLoggerOptions
            {
                FileName = "fms-",
                FileSizeLimit = Constants.MaxLogFileSize,
                LogDirectory = Configuration.GetSection("Logging").GetSection("LogDirs")[OsName + "-log"],
                ShouldBackupLogs = bool.Parse(Configuration.GetSection("Logging")["ShouldBackupLogs"]),
                BackupLogDir = Configuration.GetSection("Logging")["LogBackupDir-" + OsName]
            };

            var opts = Options.Create(option);
            services.AddSingleton<ILoggerProvider>(loggerProvider => new FileLoggerProvider(opts));

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
		                .AllowAnyMethod()
		                .AllowAnyHeader()
		                .AllowAnyOrigin()
		                .AllowCredentials();
	    }));

	   
            services.AddSignalR();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            services.AddSingleton<IFileLoggerService, FileLoggerService>();

            services.AddDistributedMemoryCache();

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
	    .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

        }

        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env,
                              IServiceProvider serviceProvider
                             )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
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

	    app.UseCors("CorsPolicy");
	    //app.UseWebSockets();

            app.UseAuthentication();
            app.UseSignalR(routes =>
            {
                routes.MapHub<StatusHub>("/hubs/status");
                routes.MapHub<FileOperationHub>("/hubs/files");
                routes.MapHub<MediaHub>("/hubs/media");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=home}/{action=index}/{id?}");
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
