using System;
using System.Data;
using System.Data.SqlClient;
using Npgsql;

namespace ManyWho.Service.Salesforce.Utils
{
    public class ConnectionManager
    {
        public static NpgsqlConnection PostgresConnection()
        {
            return ConnectionManager.PostgresConnection(SettingUtils.GetStringSetting(SettingUtils.SETTING_DATABASE_HOST),
                SettingUtils.GetStringSetting(SettingUtils.SETTING_DATABASE_NAME),
                SettingUtils.GetStringSetting(SettingUtils.SETTING_DATABASE_USERNAME),
                SettingUtils.GetStringSetting(SettingUtils.SETTING_DATABASE_PASSWORD));
        }

        public static NpgsqlConnection PostgresConnection(string host, string name, string username, string password)
        {
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder();
            connectionStringBuilder.SslMode = SslMode.Prefer;

            connectionStringBuilder.Host = host;
            connectionStringBuilder.Database = name;
            connectionStringBuilder.MinPoolSize = 5;
            connectionStringBuilder.MaxPoolSize = 7;
            connectionStringBuilder.Username = username;
            connectionStringBuilder.Password = password;

            return new NpgsqlConnection(connectionStringBuilder.ToString());
        }

        public static IDbDataParameter CreateDbParameter(String parameter, DbType type, Int32 size, Boolean internationalized)
        {
            SqlParameter sqlParameter = null;

            if (type == DbType.String)
            {
                if (size < 0)
                {
                    sqlParameter = new SqlParameter(parameter, SqlDbType.NText);
                }
                else if (internationalized == true)
                {
                    sqlParameter = new SqlParameter(parameter, SqlDbType.NVarChar, size);
                }
                else
                {
                    sqlParameter = new SqlParameter(parameter, SqlDbType.VarChar, size);
                }
            }
            else if (type == DbType.Boolean)
            {
                sqlParameter = new SqlParameter(parameter, SqlDbType.Bit);
            }
            else if (type == DbType.Guid)
            {
                sqlParameter = new SqlParameter(parameter, SqlDbType.UniqueIdentifier);
            }
            else if (type == DbType.DateTime)
            {
                sqlParameter = new SqlParameter(parameter, SqlDbType.DateTime);
            }
            else
            {
                throw new NotImplementedException("DbType cannot be found for connection manager");
            }

            return sqlParameter;
        }
    }
}
