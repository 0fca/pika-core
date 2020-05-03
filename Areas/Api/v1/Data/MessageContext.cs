using Microsoft.EntityFrameworkCore;

namespace PikaCore.Areas.Api.v1.Data
{
    public class MessageContext : DbContext
    {
        private readonly string _connString =
            "Server=localhost;Database=pikadb;User Id=postgres;Password=Il0v3DnYk@mu(h;Port=5434;";

        public MessageContext()
        {
        }

        public MessageContext(DbContextOptions<MessageContext> contextOptionsBuilder) : base(contextOptionsBuilder)
        {
            
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {           
            optionsBuilder.UseNpgsql(_connString);
        }

        public DbSet<MessageEntity> Messages { get; set; }
        public DbSet<IssueEntity> Issues { get; set; }
    }
}