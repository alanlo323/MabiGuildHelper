using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Db.Entity
{
    public class News : BaseEntity
    {
        public string Url { get; set; }
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Base64Snapshot { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is News news)
            {
                if (Url != news.Url) return false;
                if (ImageUrl != news.ImageUrl) return false;
                if (Title != news.Title) return false;
                if (Content != news.Content) return false;
                if (PublishDate != news.PublishDate) return false;
                return true;
            }
            return base.Equals(obj);
        }
    }
}
