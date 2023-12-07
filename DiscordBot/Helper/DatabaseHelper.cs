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
using Microsoft.EntityFrameworkCore.Query;
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
                    new()
                    {
                        GuildId = 607932041572646932,
                        //ErinnTimeChannelId = 1165535969798258688,
                        //ErinnTimeMessageId = 1165955533493256222,
                        //DailyEffectChannelId = 1165931046584455188,
                        //DailyEffectMessageId = 1166012004398542859
                    },

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

                List<GuildUserSetting> settingList2 = new()
                {
                    //new()
                    //{
                    //    GuildId = 607932041572646932,
                    //    UserId = 170721070976860161,
                    //    InstanceReminderSettings = new List<InstanceReminderSetting>() {
                    //        new() {
                    //            UserId = 170721070976860161 ,
                    //            InstanceReminderId = 3,
                    //        },
                    //        new() {
                    //            UserId = 170721070976860161 ,
                    //            InstanceReminderId = 4,
                    //        },
                    //    },
                    //},
                };

                await _appDbContext.AddRangeAsync(settingList);
                await _appDbContext.AddRangeAsync(settingList2);
                await SaveChange();

                _logger.LogInformation($"Database re-created and inserted with base record");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task EnsureDatabaseReady()
        {
            try
            {
                await _appDbContext.Database.MigrateAsync();
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

        private async Task<T> GetOrCreateEntity<T>(Expression<Func<T, bool>> whereExpression, Dictionary<string, object> defaultPropertyValues = null, List<Expression<Func<T, object>>>? includeExpressions = null) where T : class, new()
        {
            IQueryable<T> query = _appDbContext
                .Set<T>()
                ;
            if (includeExpressions?.Count > 0)
            {
                foreach (var includeExpression in includeExpressions)
                {
                    query = query.Include(includeExpression);
                }
                //_ = query.ToList(); //  Work around for include not working
            }
            T? t = query.Where(whereExpression).FirstOrDefault();
            t ??= await CreateEntity<T>(defaultPropertyValues);
            return t;
        }

        private async Task<T> CreateEntity<T>(Dictionary<string, object> defaultPropertyValues) where T : class, new()
        {
            T t = new();
            if (defaultPropertyValues != null)
            {
                foreach (KeyValuePair<string, object> item in defaultPropertyValues)
                {
                    t.SetProperty(item.Key, item.Value);
                }
            }
            await _appDbContext.AddAsync(t);
            return t;
        }

        public async Task<T> GetOrCreateEntityByKeys<T>(Dictionary<string, object> primaryKeys, List<string>? includeProperties = null) where T : class, new()
        {
            var whereExpression = BuildWhereEqualExpression<T>(primaryKeys);
            var includeExpressions = BuildIncludeExpression<T>(includeProperties);
            var entity = await GetOrCreateEntity(whereExpression, primaryKeys, includeExpressions: includeExpressions);
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
                var property = Expression.PropertyOrField(parameter, item.Key);
                var constant = Expression.Constant(item.Value, item.Value.GetType());
                var equal = Expression.Equal(property, constant);
                whereClause = whereClause == null ? equal : Expression.And(whereClause, equal);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(whereClause, parameter);
            return lambda;
        }

        public List<Expression<Func<T, object>>> BuildIncludeExpression<T>(List<string>? includeProperties)
        {
            List<Expression<Func<T, object>>> expressions = new();
            if (includeProperties == null || includeProperties.Count == 0)
            {
                return expressions;
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            foreach (var item in includeProperties)
            {
                var property = Expression.Property(parameter, item);
                var lambda = Expression.Lambda<Func<T, object>>(property, parameter);
                expressions.Add(lambda);
            }
            return expressions;
        }
    }
}
