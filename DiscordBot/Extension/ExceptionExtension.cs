using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Extension
{
    public static class ExceptionExtension
    {
        public static string LogException(this ILogger logger, Exception ex)
        {
            StringBuilder errorMsgBuilder = new();
            Exception currentException = ex;
            do
            {
                errorMsgBuilder.AppendLine($"{currentException?.Message}");
                currentException = currentException?.InnerException;
            } while (currentException != null);
            string errorMsg = errorMsgBuilder.ToString();
            logger.LogError(ex, errorMsg);

            return errorMsg;
        }
    }
}
