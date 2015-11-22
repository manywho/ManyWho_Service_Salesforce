using Npgsql;
using NpgsqlTypes;
using System;
using System.Threading.Tasks;

namespace ManyWho.Service.Salesforce.Utils
{
    public class PostgresDatabase : IDisposable
    {
        public const string DBTYPE_BOOLEAN = "BOOLEAN";
        public const string DBTYPE_STRING = "STRING";
        public const string DBTYPE_DATETIME = "DATETIME";
        public const string DBTYPE_UNIQUEIDENTIFIER = "UniqueIdentifier";
        public const string DBTYPE_JSON = "JSON";
        public const string DBTYPE_BIGINT = "BIGINT";
        public const string DBTYPE_INTEGER = "INTEGER";

        private NpgsqlConnection Connection { get; set; }
        private NpgsqlTransaction Transaction { get; set; }
        private NpgsqlCommand Command { get; set; }

        public PostgresDatabase()
        {
            this.Initialize(ConnectionManager.PostgresConnection());
        }

        public string ParameterPrefix
        {
            get { return ":"; }
        }

        private void Initialize(NpgsqlConnection connection)
        {
            this.Connection = connection;

            try
            {
                this.Connection.Open();
            }
            catch (Exception)
            {
                throw;
            }

            this.Transaction = this.Connection.BeginTransaction();
        }

        public void CreateCommand()
        {
            this.Command = new NpgsqlCommand();
            this.Command.Connection = this.Connection;
            this.Command.Transaction = this.Transaction;
        }

        public bool ContainsParameter(string name)
        {
            if (this.Command != null && this.Command.Parameters != null)
            {
                return this.Command.Parameters.Contains(name);
            }

            return false;
        }

        public void AddDbParameter(string parameter, string dbType, int size, bool internationalized, object value)
        {
            NpgsqlParameter sqlParameter = null;

            if (dbType.Equals(DBTYPE_STRING, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                if (size < 0)
                {
                    sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Text);
                }
                else
                {
                    sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Varchar, size);
                }
            }
            else if (dbType.Equals(DBTYPE_BOOLEAN, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Boolean);
                value = Convert.ToBoolean(value);
            }
            else if (dbType.Equals(DBTYPE_UNIQUEIDENTIFIER, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Uuid);

                if (value != null && value is string)
                {
                    value = Guid.Parse(value.ToString());
                }
            }
            else if (dbType.Equals(DBTYPE_DATETIME, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Timestamp);
            }
            else if (dbType.Equals(DBTYPE_JSON, StringComparison.InvariantCultureIgnoreCase))
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Jsonb);
            }
            else if (dbType.Equals(DBTYPE_INTEGER, StringComparison.InvariantCultureIgnoreCase))
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Integer);
            }
            else if (dbType.Equals(DBTYPE_BIGINT, StringComparison.InvariantCultureIgnoreCase))
            {
                sqlParameter = new NpgsqlParameter(parameter, NpgsqlDbType.Bigint);
            }
            else
            {
                throw new NotImplementedException("DbType cannot be found for database implementation: " + dbType);
            }

            // Check to see if we have a value to store and assign the parameter value accordingly
            if (value != null)
            {
                if (value is string)
                {
                    if (!string.IsNullOrWhiteSpace(value.ToString()))
                    {
                        sqlParameter.Value = value;
                    }
                    else
                    {
                        sqlParameter.Value = DBNull.Value;
                    }
                }
                else
                {
                    sqlParameter.Value = value;
                }
            }
            else
            {
                sqlParameter.Value = DBNull.Value;
            }

            this.Command.Parameters.Add(sqlParameter);
        }

        public void ExecuteCommand(string command)
        {
            this.Command.CommandText = command;
            this.Command.ExecuteNonQuery();
        }

        public async Task ExecuteCommandAsync(string command)
        {
            this.Command.CommandText = command;
            await this.Command.ExecuteNonQueryAsync();
        }

        public PostgresDatabaseReader ExecuteQuery(string query)
        {
            this.Command.CommandText = query;

            return new PostgresDatabaseReader(this.Command.ExecuteReader());
        }

        public async Task<PostgresDatabaseReader> ExecuteQueryAsync(string query)
        {
            this.Command.CommandText = query;

            return new PostgresDatabaseReader(await this.Command.ExecuteReaderAsync() as NpgsqlDataReader);
        }

        public async Task<object> ExecuteScalarAsync(string query)
        {
            Command.CommandText = query;

            return await Command.ExecuteScalarAsync();
        }

        public void Commit()
        {
            this.Transaction.Commit();
        }

        public void Rollback()
        {
            if (this.Transaction != null)
            {
                this.Transaction.Rollback();
            }
        }

        public void Dispose()
        {
            try
            {
                if (this.Transaction != null)
                {
                    this.Transaction.Dispose();
                    this.Transaction = null;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (this.Connection != null)
                {
                    // Make sure it is absolutely dead
                    this.Connection.Dispose();
                    this.Connection = null;
                }
            }
        }
    }
}
