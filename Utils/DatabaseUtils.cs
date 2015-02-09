using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using ManyWho.Service.Salesforce.Utils;

namespace ManyWho.Service.ManyWho.Utils.Utils
{
    public class DatabaseUtils
    {
        public static SqlConnection SqlConnection()
        {
            SqlConnection sqlConnection;
            SqlConnectionStringBuilder sqlConnectionStringBuilder;

            sqlConnectionStringBuilder = new SqlConnectionStringBuilder();
            sqlConnectionStringBuilder.DataSource = SettingUtils.GetStringSetting(SettingUtils.APP_SETTING_DATASOURCE);
            sqlConnectionStringBuilder.InitialCatalog = SettingUtils.GetStringSetting(SettingUtils.APP_SETTING_DATABASE_NAME);
            sqlConnectionStringBuilder.MinPoolSize = 5;
            sqlConnectionStringBuilder.ConnectTimeout = 600;

            if (SettingUtils.GetBooleanSetting(SettingUtils.APP_SETTING_IS_DEBUG) == false)
            {
                sqlConnectionStringBuilder.Encrypt = true;
                sqlConnectionStringBuilder.TrustServerCertificate = true;
                sqlConnectionStringBuilder.UserID = SettingUtils.GetStringSetting(SettingUtils.APP_SETTING_DATABASE_USERNAME);
                sqlConnectionStringBuilder.Password = SettingUtils.GetStringSetting(SettingUtils.APP_SETTING_DATABASE_PASSWORD);
            }
            else
            {
                sqlConnectionStringBuilder.IntegratedSecurity = true;
            }

            sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ToString());

            return sqlConnection;
        }
    }
}