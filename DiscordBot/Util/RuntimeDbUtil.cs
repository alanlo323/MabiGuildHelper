using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Util
{
    public class RuntimeDbUtil
    {
        private static Dictionary<string, object> RuntimeDbDict { get; set; } = [];
        public static Dictionary<object, object> DefaultRuntimeDb { get; set; } = [];

        public static Dictionary<T1, T2> GetRuntimeDb<T1, T2>(string dbName) where T1 : notnull
        {
            Dictionary<T1, T2> db;
            if (!RuntimeDbDict.TryGetValue(dbName, out object dbInRam))
            {
                dbInRam = new Dictionary<T1, T2>();
                RuntimeDbDict[dbName] = dbInRam;
            }
            db = dbInRam as Dictionary<T1, T2>;
            return db!;
        }
    }
}
