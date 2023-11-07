using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Db.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DiscordBot.Db
{
    public class AppDbContext : DbContext
    {
        public static readonly string ConnectionStringName = "MabiDb";

        public DbSet<GuildSetting> GuildSettings { get; set; }

        public AppDbContext(DbContextOptions option) : base(option) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (EntityEntry entityEntry in ChangeTracker.Entries())
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (entityEntry.Entity is BaseEntity baseEntity)
                {
                    DateTime now = DateTime.Now;
                    switch (entityEntry.State)
                    {
                        case EntityState.Added:
                        case EntityState.Modified:
                            baseEntity.CreatedAt ??= now;
                            baseEntity.UpdatedAt = now;
                            break;
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
