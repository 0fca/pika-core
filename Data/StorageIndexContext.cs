using Microsoft.EntityFrameworkCore;
using FMS2.Models;

namespace FMS2.Data
{
    public class StorageIndexContext : DbContext
    {
        public StorageIndexContext(DbContextOptions<StorageIndexContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public DbSet<StorageIndexRecord> IndexStorage { get; set; }
    }
}