using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.DataObject;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Extension
{
    public static class ObjectExtension
    {
        private static readonly JsonSerializerSettings NewtonsoftSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
        };

        private static readonly JsonSerializerOptions SystemTextJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true,
        };

        public static string SerializeWithNewtonsoft<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, NewtonsoftSettings);
        }

        public static T? DeserializeWithNewtonsoft<T>(this string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, NewtonsoftSettings);
        }

        public static string Serialize<T>(this T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, SystemTextJsonOptions);
        }

        public static T? Deserialize<T>(this string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, SystemTextJsonOptions);
        }

        public static T SetProperty<T>(this T obj, string propertyName, object value)
        {
            var property = obj?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            return obj;
        }

        public static T? GetProperty<T>(this object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            if (property != null && property.CanRead)
            {
                return (T)property.GetValue(obj);
            }
            return default;
        }

        public static T GetField<T>(this object obj, string fieldName, bool throwException = false)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (fieldInfo != null)
            {
                object value = fieldInfo.GetValue(obj);
                if (value is T)
                {
                    return (T)value;
                }
                throw new InvalidCastException($"Field '{fieldName}' is not of type {typeof(T).FullName}");
            }
            if (throwException) throw new ArgumentException($"Field '{fieldName}' not found on type '{obj.GetType().FullName}'");
            return default;
        }
    }
}
