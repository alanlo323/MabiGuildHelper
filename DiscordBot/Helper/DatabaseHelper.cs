using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Commands;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Helper
{
    public class DatabaseHelper
    {
        private ILogger<Bot> _logger;
        private IServiceProvider _serviceProvider;
        private AppDbContext _appDbContext;

        public DatabaseHelper(ILogger<Bot> logger, IServiceProvider serviceProvider, AppDbContext appDbContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appDbContext = appDbContext;
        }

        public async Task ResetDatabase()
        {
            try
            {
                await _appDbContext.Database.EnsureDeletedAsync();
                await _appDbContext.Database.EnsureCreatedAsync();

                //  erinntime
                List<GuildSetting> settingList = new()
                {
                    // Information
                    new()
                    {
                        GuildId = 607932041572646932,
                        ErinnTimeChannelId = 1165535969798258688,
                        ErinnTimeMessageId = 1165955533493256222,
                        DailyEffectChannelId = 1165931046584455188,
                        DailyEffectMessageId = 1166012004398542859
                    },

                    // 夏夜
                    new()
                    {
                        GuildId = 1058732396998033428,
                        ErinnTimeChannelId = 1166045923865006180,
                        ErinnTimeMessageId = 1166050873395400815,
                        DailyEffectChannelId = 1166044497533214760,
                        DailyEffectMessageId = 1166050823772577864
                    },
                };

                await _appDbContext.AddRangeAsync(settingList);
                await _appDbContext.SaveChangesAsync();

                _logger.LogInformation($"Database re-created and inserted with base record");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
