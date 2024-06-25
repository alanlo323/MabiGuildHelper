using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class ImgurConfig
    {
        public const string SectionName = "Imgur";

        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string RefreshToken { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(RefreshToken);
        }
    }
}
