﻿// <auto-generated />
using System;
using DiscordBot.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DiscordBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240229091248_Crombas")]
    partial class Crombas
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.2");

            modelBuilder.Entity("DiscordBot.Db.Entity.GlobalSetting", b =>
                {
                    b.Property<string>("Key")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeValue")
                        .HasColumnType("TEXT");

                    b.Property<double?>("DoubleValue")
                        .HasColumnType("REAL");

                    b.Property<int?>("IntValue")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StringValue")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("UlongValue")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("GlobalSettings");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildNewsOverride", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("NewsId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Base64Snapshot")
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemTag")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ReleatedMessageUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "NewsId");

                    b.ToTable("GuildNewsOverrides");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildSetting", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("CrombasHelperChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DailyDungeonInfoChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DailyDungeonInfoMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DailyEffectChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DailyEffectMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("DataScapingNewsChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ErinnTimeChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ErinnTimeMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderMessageIdBattle")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderMessageIdLife")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderMessageIdMisc")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderMessageIdOneDay")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("InstanceResetReminderMessageIdToday")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildUserSetting", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "UserId");

                    b.ToTable("GuildUserSettings");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.InstanceReminderSetting", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InstanceReminderId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId", "UserId", "InstanceReminderId");

                    b.ToTable("InstanceReminderSettings");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.News", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Base64Snapshot")
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemTag")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("PublishDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("News");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildNewsOverride", b =>
                {
                    b.HasOne("DiscordBot.Db.Entity.GuildSetting", "GuildSetting")
                        .WithMany("GuildNewsOverrides")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSetting");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildUserSetting", b =>
                {
                    b.HasOne("DiscordBot.Db.Entity.GuildSetting", "GuildSetting")
                        .WithMany("GuildUserSettings")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildSetting");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.InstanceReminderSetting", b =>
                {
                    b.HasOne("DiscordBot.Db.Entity.GuildUserSetting", "GuildUserSetting")
                        .WithMany("InstanceReminderSettings")
                        .HasForeignKey("GuildId", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildUserSetting");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildSetting", b =>
                {
                    b.Navigation("GuildNewsOverrides");

                    b.Navigation("GuildUserSettings");
                });

            modelBuilder.Entity("DiscordBot.Db.Entity.GuildUserSetting", b =>
                {
                    b.Navigation("InstanceReminderSettings");
                });
#pragma warning restore 612, 618
        }
    }
}
