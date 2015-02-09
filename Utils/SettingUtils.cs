using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Configuration;

namespace ManyWho.Service.Salesforce.Utils
{
    public class SettingUtils
    {
        public const String APP_SETTING_DATASOURCE = "PluginManyWhoUtils.DataSource";
        public const String APP_SETTING_DATABASE_NAME = "PluginManyWhoUtils.DatabaseName";
        public const String APP_SETTING_DATABASE_USERNAME = "PluginManyWhoUtils.DatabaseUsername";
        public const String APP_SETTING_DATABASE_PASSWORD = "PluginManyWhoUtils.DatabasePassword";
        public const String APP_SETTING_IS_DEBUG = "PluginManyWhoUtils.Debug";

        public static String GetStringSetting(String setting)
        {
            return ConfigurationManager.AppSettings.Get(setting);
        }

        public static Boolean GetBooleanSetting(String setting)
        {
            Boolean value = false;

            Boolean.TryParse(GetStringSetting(setting), out value);

            return value;
        }
    }
}