﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Util;

namespace DiscordBot.DataEntity
{
    public class DailyDungeonContainer
    {
        public List<DailyDungeonInfo> Infos { get; set; }
        public string Html { get; set; }
        public Image Image{ get => MiscUtil.ConvertHtmlToImage(Html).Result; }

        private FileInfo _imageTempFile;

        public FileInfo GetImageTempFile()
        {
            if (_imageTempFile == null)
            {
                string tempFilePath = Path.GetTempFileName().Replace("tmp", "png");
                _imageTempFile = new FileInfo(tempFilePath);
            }

            try
            {
                Image.Save(_imageTempFile.FullName);
            }
            catch (Exception)
            {
            }

            return _imageTempFile;
        }
    }
}
