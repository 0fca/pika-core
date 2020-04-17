using Microsoft.EntityFrameworkCore;
using PikaCore.Models;

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