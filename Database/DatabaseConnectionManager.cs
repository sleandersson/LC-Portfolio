using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;


namespace LC_Portfolio.Database
{
    public static class DatabaseConnectionManager
    {
        private static readonly List<SqliteConnection> openConnections = new List<SqliteConnection>();

        public static SqliteConnection OpenConnection(string connectionString)
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            openConnections.Add(connection);
            return connection;
        }

        public static void CloseAllConnections()
        {
            foreach (var connection in openConnections)
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
            openConnections.Clear(); // Clear the list after closing all connections
        }
    }

}
