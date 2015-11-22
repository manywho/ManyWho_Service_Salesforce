using System;
using ManyWho.Flow.SDK;

namespace ManyWho.Service.Salesforce.Utils
{
    public class SettingUtils
    {
        public const string SETTING_DATABASE_HOST = "DatabaseHost";
        public const string SETTING_DATABASE_NAME = "DatabaseName";
        public const string SETTING_DATABASE_USERNAME = "DatabaseUsername";
        public const string SETTING_DATABASE_PASSWORD = "DatabasePassword";

        public const string SETTING_CDN_BASE_PATH = "BasePathCdn";
        public const string SETTING_SERVER_BASE_PATH = "BasePathServer";

        public const string SETTING_EMAIL_FROM = "EmailFrom";
        public const string SETTING_EMAIL_USERNAME = "EmailUsername";
        public const string SETTING_EMAIL_PASSWORD = "EmailPassword";
        public const string SETTING_EMAIL_SMTP = "EmailSmtp";

        public static Boolean IsDebugging(String mode)
        {
            Boolean isDebugging = false;

            if (string.IsNullOrWhiteSpace(mode) == false &&
                (mode.Equals(ManyWhoConstants.MODE_DEBUG, StringComparison.OrdinalIgnoreCase) == true ||
                 mode.Equals(ManyWhoConstants.MODE_DEBUG_STEPTHROUGH, StringComparison.OrdinalIgnoreCase) == true))
            {
                isDebugging = true;
            }

            return isDebugging;
        }

        public static string GetStringSetting(String setting)
        {
            return Startup.Configuration[setting];
        }

        public static Boolean GetBooleanSetting(String setting)
        {
            Boolean value = false;

            Boolean.TryParse(GetStringSetting(setting), out value);

            return value;
        }
    }
}