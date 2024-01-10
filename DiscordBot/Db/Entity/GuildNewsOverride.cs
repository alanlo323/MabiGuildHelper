using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DiscordBot.Db.Entity
{
    public class GuildNewsOverride : BaseEntity
    {
        // Parent
        public GuildSetting GuildSetting { get; set; }

        public ulong GuildId { get; set; }
        public string Url { get; set; }
        public ItemTag? ItemTag { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ReleatedMessageUrl { get; set; }

        public static GuildNewsOverride CloneFromNews(News news)
        {
            GuildNewsOverride guildNewsOverride = new();
            // use reflection to copy all properties
            foreach (var property in typeof(GuildNewsOverride).GetProperties())
            {
                if (property.CanWrite)
                {
                    property.SetValue(guildNewsOverride, news.GetProperty<object>(property.Name));
                }
            }

            return guildNewsOverride;
        }
    }
}
