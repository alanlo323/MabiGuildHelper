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
    public class ButtonHandlerHelper
    {
        private IServiceProvider _serviceProvider;

        public ButtonHandlerHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<IBaseButtonHandler> GetButtonHandlerList()
        {
            return _serviceProvider.GetServices<IBaseButtonHandler>().ToList();
        }

        public IBaseButtonHandler GetButtonHandler(string id)
        {
            return _serviceProvider.GetServices<IBaseButtonHandler>().Single(x => x.Id == id);
        }

        public IBaseButtonHandler GetButtonHandler<T>() where T : IBaseButtonHandler
        {
            return _serviceProvider.GetServices<IBaseButtonHandler>().Single(x => x.GetType() == typeof(T));
        }
    }
}
