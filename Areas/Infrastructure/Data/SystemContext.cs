using Microsoft.EntityFrameworkCore;

namespace PikaCore.Areas.Infrastructure.Data
{
    public class SystemContext : DbContext
    {
            public SystemContext(DbContextOptions<SystemContext> contextOptionsBuilder) : base(contextOptionsBuilder)
            {
            
            }

            public DbSet<SystemDescriptor> Systems { get; set; }
            public DbSet<SystemDependency> SystemDependencies { get; set; }
            public DbSet<MessageEntity> Messages { get; set; }
            public DbSet<IssueEntity> Issues { get; set; }
    }
}