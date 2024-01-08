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
    public class SelectMenuHandlerHelper(IServiceProvider serviceProvider)
    {
        public List<IBaseSelectMenuHandler> GetSelectMenuHandlerList()
        {
            return serviceProvider.GetServices<IBaseSelectMenuHandler>().ToList();
        }

        public IBaseSelectMenuHandler GetSelectMenuHandler(string id)
        {
            return serviceProvider.GetServices<IBaseSelectMenuHandler>().Single(x => x.Id == id);
        }

        public IBaseSelectMenuHandler GetSelectMenuHandler<T>() where T : IBaseSelectMenuHandler
        {
            return serviceProvider.GetServices<IBaseSelectMenuHandler>().Single(x => x.GetType() == typeof(T));
        }
    }
}
