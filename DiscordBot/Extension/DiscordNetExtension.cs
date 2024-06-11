using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Util;

namespace DiscordBot.Extension
{
    public static class DiscordNetExtension
    {
        public static async Task<FileInfo?> DownloadFile(this Attachment attachment)
        {
            try
            {
                if (attachment == null) return null;
                string url = attachment.ProxyUrl;
                FileInfo fileInfo = new(Path.GetTempFileName());
                // download file from url
                await MiscUtil.DownloadFileAsync(url, fileInfo.FullName);
                return fileInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
