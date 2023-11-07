using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Helper
{
    public class DatabaseHelper
    {
        private ILogger<DatabaseHelper> _logger;
        private IServiceProvider _serviceProvider;
        private AppDbContext _appDbContext;

        public DatabaseHelper(ILogger<DatabaseHelper> logger, IServiceProvider serviceProvider, AppDbContext appDbContext)
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
                    //// Information
                    //new()
                    //{
                    //    GuildId = 607932041572646932,
                    //    ErinnTimeChannelId = 1165535969798258688,
                    //    ErinnTimeMessageId = 1165955533493256222,
                    //    DailyEffectChannelId = 1165931046584455188,
                    //    DailyEffectMessageId = 1166012004398542859
                    //},

                    //// 夏夜
                    //new()
                    //{
                    //    GuildId = 1058732396998033428,
                    //    ErinnTimeChannelId = 1166045923865006180,
                    //    ErinnTimeMessageId = 1166050873395400815,
                    //    DailyEffectChannelId = 1166044497533214760,
                    //    DailyEffectMessageId = 1166050823772577864
                    //},
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

        public async Task<int> SaveChange()
        {
            return await _appDbContext.SaveChangesAsync();
        }

        private async Task<T> GetOrCreateEntity<T>(Expression<Func<T, bool>> whereExpression, Dictionary<string, object> defaultPropertyValues = null) where T : class, new()
        {
            T? t = await _appDbContext
                .Set<T>()
                .Where(whereExpression)
                .SingleOrDefaultAsync()
                ;

            if (t == null)
            {
                t = new T();
                if (defaultPropertyValues != null)
                {
                    foreach (KeyValuePair<string, object> item in defaultPropertyValues)
                    {
                        t.SetProperty(item.Key, item.Value);
                    }
                }
                await _appDbContext.AddAsync(t);
            }

            return t;
        }

        public async Task<T> GetOrCreateEntityByKeys<T>(Dictionary<string, object> primaryKeys) where T : class, new()
        {
            var expression = BuildWhereEqualExpression<T>(primaryKeys);
            var entity = await GetOrCreateEntity(expression, primaryKeys);
            await SaveChange();
            return entity;
        }

        public Expression<Func<T, bool>> BuildWhereEqualExpression<T>(Dictionary<string, object> primaryKeys)
        {
            if (primaryKeys == null || primaryKeys.Count == 0)
            {
                throw new ArgumentNullException(nameof(primaryKeys));
            }

            Expression whereClause = null;
            var parameter = Expression.Parameter(typeof(T), "x");
            foreach (var item in primaryKeys)
            {
                var property = Expression.Property(parameter, item.Key);
                var constant = Expression.Constant(item.Value);
                var equal = Expression.Equal(property, constant);
                whereClause = whereClause == null ? equal : Expression.And(whereClause, equal);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(whereClause, parameter);
            return lambda;
        }
    }
}
