using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Db.Entity;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DiscordBot.Db
{
    public class AppDbContext(DbContextOptions option) : DbContext(option)
    {
        public static readonly string ConnectionStringName = "MabiDb";

        public DbSet<GuildSetting> GuildSettings { get; set; }
        public DbSet<GuildUserSetting> GuildUserSettings { get; set; }
        public DbSet<GuildNewsOverride> GuildNewsOverrides { get; set; }
        public DbSet<InstanceReminderSetting> InstanceReminderSettings { get; set; }
        public DbSet<DailyVipGiftReminderSetting> DailyVipGiftReminderSettings { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<GlobalSetting> GlobalSettings { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<DiscordDbMessage> Messages { get; set; }

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
            modelBuilder.Entity<GuildSetting>()
                .HasMany(e => e.GuildUserSettings)
                .WithOne(e => e.GuildSetting)
                .HasForeignKey(e => e.GuildId)
                ;
            modelBuilder.Entity<GuildSetting>()
                .HasMany(e => e.GuildNewsOverrides)
                .WithOne(e => e.GuildSetting)
                .HasForeignKey(e => e.GuildId)
                ;

            modelBuilder.Entity<GuildUserSetting>()
                .HasKey(e => new { e.GuildId, e.UserId })
                ;
            modelBuilder.Entity<GuildUserSetting>()
                .HasMany(e => e.InstanceReminderSettings)
                .WithOne(e => e.GuildUserSetting)
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                ;

            modelBuilder.Entity<GuildNewsOverride>()
                .HasKey(e => new { e.GuildId, e.NewsId })
                ;

            modelBuilder.Entity<InstanceReminderSetting>()
                .HasKey(e => new { e.GuildId, e.UserId, e.ReminderId })
                ;
            modelBuilder.Entity<InstanceReminderSetting>()
                .HasOne(e => e.GuildUserSetting)
                .WithMany(e => e.InstanceReminderSettings)
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                ;

            modelBuilder.Entity<DailyVipGiftReminderSetting>()
                .HasKey(e => new { e.GuildId, e.UserId, e.ReminderId })
                ;
            modelBuilder.Entity<DailyVipGiftReminderSetting>()
                .HasOne(e => e.GuildUserSetting)
                .WithMany(e => e.DailyVipGiftReminderSettings)
                .HasForeignKey(e => new { e.GuildId, e.UserId })
                ;

            modelBuilder.Entity<News>()
                .HasKey(e => e.Id)
                ;
            modelBuilder.Entity<News>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd()
                ;

            modelBuilder.Entity<GlobalSetting>()
                .HasKey(e => e.Key)
                ;

            modelBuilder.Entity<Conversation>()
                .HasKey(e => e.Id)
                ;
            modelBuilder.Entity<Conversation>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd()
                ;

            modelBuilder.Entity<DiscordDbMessage>()
                .HasKey(e => e.Id)
                ;
        }
    }
}
