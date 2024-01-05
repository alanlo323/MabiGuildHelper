using System.Drawing;

namespace DiscordBot.Util
{
    public class ImageUtil
    {
        public static string ImageToBase64(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}