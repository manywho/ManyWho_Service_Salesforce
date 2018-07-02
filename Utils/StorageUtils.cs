using System;
using ManyWho.Flow.SDK.Security;
using Newtonsoft.Json;

namespace ManyWho.Service.Salesforce.Utils
{
    public class StorageUtils
    {
        public static String GetStoredJson(String key)
        {
            String json = null;

            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("Id", PostgresDatabase.DBTYPE_STRING, 255, false, key);

                using (PostgresDatabaseReader databaseReader = postgresDatabase.ExecuteQuery(string.Format("SELECT Json FROM StoredJson WHERE Id = {0}Id LIMIT 1", postgresDatabase.ParameterPrefix)))
                {
                    if (databaseReader.HasRecords() == true)
                    {
                        while (databaseReader.GetNextRecord() == true)
                        {
                            json = databaseReader.GetString(0);
                            break;
                        }
                    }
                }
            }

            return json;
        }

        public static void RemoveStoredJson(String key)
        {
            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("Id", PostgresDatabase.DBTYPE_STRING, 255, false, key);

                postgresDatabase.ExecuteCommand(string.Format("DELETE FROM StoredJson WHERE Id = {0}Id", postgresDatabase.ParameterPrefix));

                postgresDatabase.Commit();
            }
        }

        public static void SetStoredJson(String key, String json)
        {
            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("Id", PostgresDatabase.DBTYPE_STRING, 255, false, key);
                postgresDatabase.AddDbParameter("Json", PostgresDatabase.DBTYPE_JSON, -1, true, json);

                if (string.IsNullOrWhiteSpace(GetStoredJson(key)) == true)
                {
                    postgresDatabase.ExecuteCommand(string.Format("INSERT INTO StoredJson (Id, Json) VALUES ({0}Id, {0}Json)", postgresDatabase.ParameterPrefix));
                }
                else
                {
                    postgresDatabase.ExecuteCommand(string.Format("UPDATE StoredJson SET Json = {0}Json WHERE Id = {0}Id", postgresDatabase.ParameterPrefix));
                }

                postgresDatabase.Commit();
            }
        }
        
        public static void RemoveRequestToken(String state)
        {
            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("State", PostgresDatabase.DBTYPE_STRING, 255, false, state);

                postgresDatabase.ExecuteCommand(string.Format("DELETE FROM RequestToken WHERE State = {0}State", postgresDatabase.ParameterPrefix));

                postgresDatabase.Commit();
            }
        }
        
        public static void SetRequestToken(String state, IAuthenticatedWho authenticatedWho)
        {
            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                String token = JsonConvert.SerializeObject(authenticatedWho);
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("State", PostgresDatabase.DBTYPE_STRING, 255, false, state);
                postgresDatabase.AddDbParameter("Token", PostgresDatabase.DBTYPE_JSON, -1, true, token);

                if (GetRequestToken(state) == null)
                {
                    postgresDatabase.ExecuteCommand(string.Format("INSERT INTO RequestToken (State, Token) VALUES ({0}State, {0}Token)", postgresDatabase.ParameterPrefix));
                }
                else
                {
                    postgresDatabase.ExecuteCommand(string.Format("UPDATE RequestToken SET Token = {0}Token WHERE State = {0}State", postgresDatabase.ParameterPrefix));
                }

                postgresDatabase.Commit();
            }
        }
        
        public static AuthenticatedWho GetRequestToken(string state)
        {
            using (PostgresDatabase postgresDatabase = new PostgresDatabase())
            {
                postgresDatabase.CreateCommand();
                postgresDatabase.AddDbParameter("State", PostgresDatabase.DBTYPE_STRING, 255, false, state);

                using (PostgresDatabaseReader databaseReader = postgresDatabase.ExecuteQuery(string.Format("SELECT Token FROM RequestToken WHERE State = {0}State LIMIT 1", postgresDatabase.ParameterPrefix)))
                {
                    if (databaseReader.HasRecords() == true)
                    {
                        while (databaseReader.GetNextRecord() == true)
                        {
                            return JsonConvert.DeserializeObject<AuthenticatedWho>(databaseReader.GetString(0));
                        }
                    }
                }
            }

            return null;
        }
    }
}