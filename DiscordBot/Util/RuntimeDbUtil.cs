using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Util
{
    public class RuntimeDbUtil
    {
        public static Dictionary<object, object> DefaultRuntimeDb { get; set; } = [];
        public static Dictionary<T1, T2> CreateRuntimeDb<T1, T2>() where T1 : notnull => [];
    }
}
