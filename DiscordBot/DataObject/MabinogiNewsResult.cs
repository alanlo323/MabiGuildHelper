using DiscordBot.Db.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.DataObject
{
    public class MabinogiNewsResult
    {
        public List<News> NewNews { get; set; }
        public List<News> UpdatedNews { get; set; }
    }
}
