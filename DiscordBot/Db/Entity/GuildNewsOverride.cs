using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;
using DiscordBot.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DiscordBot.Db.Entity
{
    public class GuildNewsOverride : BaseEntity
    {
        // Parent
        public GuildSetting GuildSetting { get; set; }
        public ulong GuildId { get; set; }
        public int NewsId { get; set; }
        public ItemTag? ItemTag { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Base64Snapshot { get; set; }
        public string? ReleatedMessageUrl { get; set; }

        [NotMapped]
        private FileInfo _snapshotTempFile;

        [NotMapped]
        public FileInfo SnapshotTempFile
        {
            get
            {
                if (_snapshotTempFile == null)
                {
                    string tempFilePath = Path.GetTempFileName().Replace("tmp", "png");
                    _snapshotTempFile = new FileInfo(tempFilePath);
                }

                try
                {
                    var image = ImageUtil.Base64ToImage(Base64Snapshot);
                    image.Save(_snapshotTempFile.FullName);
                }
                catch (Exception)
                {
                    // Save a blank image
                    using var image = new Bitmap(1, 1);
                    image.Save(_snapshotTempFile.FullName);
                }

                return _snapshotTempFile;
            }
        }

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
            guildNewsOverride.NewsId = news.Id;

            return guildNewsOverride;
        }
    }
}
