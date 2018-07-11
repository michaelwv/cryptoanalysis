using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace TradingAnalytics.DataAccess
{
    public class MySqlDataAccess
    {
        private MySqlConnection connection;

        public MySqlDataAccess()
        {
            connection = new MySqlConnection();

            MySqlConnectionStringBuilder mySqlConnectionString = new MySqlConnectionStringBuilder() {
                Server = Properties.Settings.Default.DbServer,
                UserID = Properties.Settings.Default.DbUserId,
                Password = Properties.Settings.Default.DbPassword,
                Database = Properties.Settings.Default.DbName
            };

            connection.ConnectionString = mySqlConnectionString.GetConnectionString(true);
        }

        public bool OpenConnection()
        {
            try
            {
                connection.Open();
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        throw new Exception("Cannot connect to server. Contact administrator !");

                    case 1045:
                        throw new Exception("Invalid username/password. Please check your connection string and try again !");
                }
                return false;
            }
            return true;
        }

        public bool CloseConnection()
        {
            try
            {
                if(connection.State != System.Data.ConnectionState.Closed)
                    connection.Close();

                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.InnerException == null)
                    throw new Exception(ex.Message);

                throw new Exception(ex.InnerException.Message);
            }
        }

        public int ExecuteStoredProcedure(string cmdText)
        {
            OpenConnection();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand()
                {
                    CommandType = System.Data.CommandType.StoredProcedure,
                    CommandText = cmdText,
                    Connection = connection
                };

                var result = mySqlCommand.ExecuteNonQuery();

                CloseConnection();

                return result;
            }
            catch (MySqlException ex)
            {
                CloseConnection();

                if (ex.InnerException == null)
                    throw new Exception(ex.Message);

                throw new Exception(ex.InnerException.Message);
            }
        }

        public MySqlDataReader ExecuteReader(string cmdText, Dictionary<string, object> arrParam)
        {
            OpenConnection();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand()
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandText = cmdText,
                    Connection = connection
                };

                List<MySqlParameter> param = new List<MySqlParameter>();

                if (arrParam != null)
                    foreach (var item in arrParam)
                        param.Add(new MySqlParameter(item.Key, item.Value));

                mySqlCommand.Parameters.AddRange(param.ToArray());

                var result = mySqlCommand.ExecuteReader();

                return result;
            }
            catch (MySqlException ex)
            {
                CloseConnection();

                if (ex.InnerException == null)
                    throw new Exception(ex.Message);

                throw new Exception(ex.InnerException.Message);
            }
        }

        public int ExecuteNonQuery(string cmdText, Dictionary<string, object> arrParam)
        {
            OpenConnection();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand()
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandText = cmdText,
                    Connection = connection
                };

                List<MySqlParameter> param = new List<MySqlParameter>();

                if (arrParam != null)
                    foreach (var item in arrParam)
                        param.Add(new MySqlParameter(item.Key, item.Value));

                mySqlCommand.Parameters.AddRange(param.ToArray());

                var result = mySqlCommand.ExecuteNonQuery();

                CloseConnection();

                return result;
            }
            catch (MySqlException ex)
            {
                CloseConnection();

                if (ex.InnerException == null)
                    throw new Exception(ex.Message);

                throw new Exception(ex.InnerException.Message);
            }
        }

        public object ExecuteScalar(string cmdText, Dictionary<string, object> arrParam)
        {
            OpenConnection();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand()
                {
                    CommandType = System.Data.CommandType.Text,
                    CommandText = cmdText,
                    Connection = connection
                };

                List<MySqlParameter> param = new List<MySqlParameter>();

                if (arrParam != null)
                    foreach (var item in arrParam)
                        param.Add(new MySqlParameter(item.Key, item.Value));

                mySqlCommand.Parameters.AddRange(param.ToArray());

                var result = mySqlCommand.ExecuteScalar();

                CloseConnection();

                return result;
            }
            catch (MySqlException ex)
            {
                CloseConnection();

                if (ex.InnerException == null)
                    throw new Exception(ex.Message);

                throw new Exception(ex.InnerException.Message);
            }
        }
    }
}
