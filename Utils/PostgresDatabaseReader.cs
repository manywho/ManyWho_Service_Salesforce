using Npgsql;
using System;

namespace ManyWho.Service.Salesforce.Utils
{
    public class PostgresDatabaseReader : IDisposable
    {
        private NpgsqlDataReader DataReader { get; set; }

        public PostgresDatabaseReader(NpgsqlDataReader dataReader)
        {
            this.DataReader = dataReader;
        }

        public bool HasRecords()
        {
            return this.DataReader.HasRows;
        }

        public Guid GetUniqueIdentifier(int index)
        {
            Guid value = Guid.Empty;

            if (this.DataReader.IsDBNull(index) == false)
            {
                value = this.DataReader.GetGuid(index);
            }

            return value;
        }

        public string GetString(int index)
        {
            String value = null;

            if (this.DataReader.IsDBNull(index) == false)
            {
                value = this.DataReader.GetString(index);
            }

            return value;
        }

        public DateTime GetDateTime(int index)
        {
            DateTime value = DateTime.Now;

            if (this.DataReader.IsDBNull(index) == false)
            {
                value = this.DataReader.GetDateTime(index);
            }

            return value;
        }

        public bool GetBoolean(int index)
        {
            bool value = false;

            if (this.DataReader.IsDBNull(index) == false)
            {
                value = this.DataReader.GetBoolean(index);
            }

            return value;
        }

        public int GetNumber(int index)
        {
            int value = 0;

            if (this.DataReader.IsDBNull(index) == false)
            {
                value = Convert.ToInt32(this.DataReader.GetValue(index));
            }

            return value;
        }

        public bool GetNextRecord()
        {
            return this.DataReader.Read();
        }

        public void Dispose()
        {
            if (this.DataReader != null && this.DataReader.IsClosed == false)
            {
                this.DataReader.Close();
            }
        }
    }
}
