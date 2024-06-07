using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.ButtonHandler;
using DiscordBot.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Helper
{
    public class ButtonHandlerHelper(IServiceProvider serviceProvider)
    {
        public List<IBaseButtonHandler> GetButtonHandlerList()
        {
            return serviceProvider.GetServices<IBaseButtonHandler>().ToList();
        }

        public IBaseButtonHandler GetButtonHandler(string id)
        {
            return serviceProvider.GetServices<IBaseButtonHandler>().Single(x => x.Ids.Any(y => y == id));
        }

        public IBaseButtonHandler GetButtonHandler<T>() where T : IBaseButtonHandler
        {
            return serviceProvider.GetServices<IBaseButtonHandler>().Single(x => x.GetType() == typeof(T));
        }
    }
}
