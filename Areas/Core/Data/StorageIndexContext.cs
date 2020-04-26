using Microsoft.EntityFrameworkCore;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Areas.Core.Data
{
    public class StorageIndexContext : DbContext
    {
        private readonly string _connString =
            "Server=localhost;Database=pikadb;User Id=postgres;Password=Il0v3DnYk@mu(h;Port=5434;";

        public StorageIndexContext()
        {
        }

        public StorageIndexContext(DbContextOptions<StorageIndexContext> options)
            : base(options)
        {
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {           
            optionsBuilder.UseNpgsql(_connString);
        }
        
        public DbSet<StorageIndexRecord> IndexStorage { get; set; }
    }
}