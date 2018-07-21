using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace WebApplicationComosDbSearch.Util
{
    public static class Config
    {
        public static T GetValue<T>(string key, T defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static T GetValue<T>(string key)
        {
            return GetValue<T>(key, default(T));
        }
    }
}