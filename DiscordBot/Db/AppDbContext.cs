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
    public class AppDbContext(DbContextOptions option) : DbContext(option)
    {
        public static readonly string ConnectionStringName = "MabiDb";

        public DbSet<GuildSetting> GuildSettings { get; set; }
        public DbSet<GuildUserSetting> GuildUserSettings { get; set; }
        public DbSet<InstanceReminderSetting> InstanceReminderSettings { get; set; }
        public DbSet<News> News { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuildSetting>()
                .HasKey(e => e.GuildId)
                ;

            modelBuilder.Entity<GuildUserSetting>()
                .HasKey(e => new { e.GuildId, e.UserId })
                ;
            modelBuilder.Entity<GuildUserSetting>()
                .HasMany(e => e.InstanceReminderSettings)
                .WithOne(e => e.GuildUserSetting)
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                ;

            modelBuilder.Entity<InstanceReminderSetting>()
                .HasKey(e => new { e.GuildId, e.UserId, e.InstanceReminderId })
                ;
            modelBuilder.Entity<InstanceReminderSetting>()
                .HasOne(e => e.GuildUserSetting)
                .WithMany(e => e.InstanceReminderSettings)
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                ;

            modelBuilder.Entity<News>()
                .HasKey(e => e.Url)
                ;
        }
    }
}
