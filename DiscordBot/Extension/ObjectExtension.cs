﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Extension
{
    public static class ObjectExtension
    {

        public static string Serialize<T>(this T obj)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                 IncludeFields = true,
            };
            return JsonSerializer.Serialize(obj, options);
        }

        public static T? Deserialize<T>(this string jsonString)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                IncludeFields = true,
            };
            return JsonSerializer.Deserialize<T>(jsonString, options);
        }

        public static T SetProperty<T>(this T obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            return obj;
        }

        public static T GetProperty<T>(this object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            if (property != null && property.CanRead)
            {
                return (T)property.GetValue(obj);
            }
            return default;
        }
    }
}
