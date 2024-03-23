using Microsoft.Data.Sqlite;
using System.Collections.Generic;
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
            openConnections.Clear(); // Clears the list after closing all connections
        }
    }
}
