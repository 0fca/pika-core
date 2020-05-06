using Microsoft.EntityFrameworkCore;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Areas.Core.Data
{
    public class StorageIndexContext : DbContext
    {
        public StorageIndexContext(DbContextOptions<StorageIndexContext> options)
            : base(options)
        {
        }

        public DbSet<StorageIndexRecord> IndexStorage { get; set; }
    }
}