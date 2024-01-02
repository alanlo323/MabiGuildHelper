using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Helper
{
    public class DataScrapingHelper(ILogger<DatabaseHelper> logger, IServiceProvider serviceProvider, AppDbContext appDbContext)
    {
    }
}
