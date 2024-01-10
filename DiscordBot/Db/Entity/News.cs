﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Migrations;
using DiscordBot.Util;

namespace DiscordBot.Db.Entity
{
    public enum ItemTag
    {
        [Description("活動")]
        act,
        [Description("系統")]
        system,
        [Description("重要")]
        important,
        [Description("更新")]
        update,
    }

    public class News : BaseEntity
    {
        public string Url { get; set; }
        public ItemTag? ItemTag { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Base64Snapshot { get; set; }

        [NotMapped]
        public bool IsUrgent => Title?.Contains("臨時維護") ?? false;

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
                }

                return _snapshotTempFile;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is News news)
            {
                if (Url != news.Url) return false;
                if (Title != news.Title) return false;
                //if (ImageUrl != news.ImageUrl) return false;
                //if (ItemTag != news.ItemTag) return false;
                //if (Content != news.Content) return false;
                //if (PublishDate != news.PublishDate) return false;
                return true;
            }
            return base.Equals(obj);
        }
    }
}
