using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBot.Helper
{
    public class CommandHelper
    {
        private IServiceProvider _serviceProvider;

        public CommandHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<IBaseCommand> GetCommandList()
        {
            return _serviceProvider.GetServices<IBaseCommand>().ToList();
        }

        public IBaseCommand GetCommand(string name)
        {
            return _serviceProvider.GetServices<IBaseCommand>().Single(x => x.Name == name);
        }
    }
}
