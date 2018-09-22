using FMS2.Data;
using FMS2.Models;
using FMS2.Providers;
using FMS2.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FMS2
{
    public class Startup
    {
        private static readonly string _osName = Controllers.Constants.OsName;
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {

            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));

            services.AddDbContext<StorageIndexContext>(options => options.UseMySql(Configuration.GetConnectionString("StorageConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
            });

            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<IZipper, ArchiveService>();
            services.AddTransient<IFileDownloader, FileService>();
            services.AddTransient<IGenerator, UrlGeneratorService>();
            var option = new FileLoggerOptions
            {
                FileName = "fms-",
                FileSizeLimit = 100 * 1024 * 1024,
                LogDirectory = Configuration.GetSection("Logging").GetSection("LogDirs")[_osName + "-log"],
                ShouldBackupLogs = Boolean.Parse(Configuration.GetSection("Logging")["ShouldBackupLogs"]),
                BackupLogDir = Configuration.GetSection("Logging")["LogBackupDir-"+_osName]
            };

            var opts = Options.Create(option);
            services.AddSingleton<ILoggerProvider>(loggerProvider => new FileLoggerProvider(opts));
            
            services.AddSingleton<IFileLoggerService, FileLoggerService>();
            IFileProvider physicalProvider = new PhysicalFileProvider(Configuration.GetSection("Paths")[_osName+"-root"]);
            services.AddSingleton(physicalProvider);
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
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

            Controllers.Constants.RootPath = Configuration.GetSection("Paths")["storage"];
            Controllers.Constants.Tmp = Configuration.GetSection("Paths")[_osName+"-tmp"];

            app.UseStaticFiles();
 
            
            app.Use(async (context, next) =>
	        {
		        await next.Invoke();

                if((context.Response.StatusCode >= 400 || context.Response.StatusCode == 204) && context.Response.StatusCode != 500){
                    context.Response.Headers.Add("code", context.Response.StatusCode.ToString());
                    context.Response.Redirect("/Home/Error", true);  
                }
	        });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();

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
