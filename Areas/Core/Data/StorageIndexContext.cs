using Microsoft.EntityFrameworkCore;
using Pika.Domain.Status.Data;
using Pika.Domain.Storage.Data;

namespace PikaCore.Areas.Core.Data;

public class StorageIndexContext : DbContext
{
    public StorageIndexContext(DbContextOptions<StorageIndexContext> options)
        : base(options)
    {
    }
        
    public DbSet<StorageIndexRecord> IndexStorage { get; set; }
    public DbSet<SystemDescriptor> Systems { get; set; }
    
    public DbSet<MessageEntity> Messages { get; set; }

}