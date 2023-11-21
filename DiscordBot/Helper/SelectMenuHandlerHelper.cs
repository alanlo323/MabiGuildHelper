using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.ButtonHandler;
using DiscordBot.Commands;
using DiscordBot.SelectMenuHandler;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Helper
{
    public class SelectMenuHandlerHelper
    {
        private IServiceProvider _serviceProvider;

        public SelectMenuHandlerHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<IBaseSelectMenuHandler> GetSelectMenuHandlerList()
        {
            return _serviceProvider.GetServices<IBaseSelectMenuHandler>().ToList();
        }

        public IBaseSelectMenuHandler GetSelectMenuHandler(string id)
        {
            return _serviceProvider.GetServices<IBaseSelectMenuHandler>().Single(x => x.Id == id);
        }

        public IBaseSelectMenuHandler GetSelectMenuHandler<T>() where T : IBaseSelectMenuHandler
        {
            return _serviceProvider.GetServices<IBaseSelectMenuHandler>().Single(x => x.GetType() == typeof(T));
        }
    }
}
