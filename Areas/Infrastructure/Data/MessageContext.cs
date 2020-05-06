using Microsoft.EntityFrameworkCore;

namespace PikaCore.Areas.Infrastructure.Data
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> contextOptionsBuilder) : base(contextOptionsBuilder)
        {
            
        }

        public DbSet<MessageEntity> Messages { get; set; }
        public DbSet<IssueEntity> Issues { get; set; }
    }
}