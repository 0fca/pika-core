using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PikaCore.Areas.Core.Models;

namespace PikaCore.Areas.Core.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly string _connString = "Server=localhost;Database=pikauserdb;User Id=postgres;Password=Il0v3DnYk@mu(h;Port=5434;";

        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {           
            optionsBuilder.UseNpgsql(_connString);
        }
    }
}