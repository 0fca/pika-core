using System.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pika.Domain.Identity.Data;
using Pika.Domain.Status.Data;
using Pika.Domain.Storage.Data;
using PikaCore.Areas.Core.Models;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore.Areas.Core.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public string ExecuteUserExportDataFunction(string userId)
        {
            using var command = Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "user_data_export";
            command.Parameters.Add(new Npgsql.NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Text)
                { Value = userId });
            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();
            var res = command.ExecuteScalar().ToString();
            return res ?? "";
        }

        public DbSet<StorageIndexRecord> IndexStorage { get; set; }

        public DbSet<SystemDescriptor> Systems { get; set; }
        public DbSet<SystemDependency> SystemDependencies { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }
        public DbSet<IssueEntity> Issues { get; set; }
    }
}