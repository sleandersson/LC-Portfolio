using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace LC_Portfolio.Database
{
    public static class DatabaseHelper
    {
        private static string dbFileName = "AppDatabase.db";
        private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbFileName);

        public static void CreateDatabase()
        {
            try
            {
                if (!File.Exists(DbPath))
                {
                    using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                    {
                        connection.Open();

                        var command = connection.CreateCommand();
                        command.CommandText =
                        @"
                        CREATE TABLE IF NOT EXISTS Users (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            FIRST_NAME TEXT NOT NULL,
                            LAST_NAME TEXT NOT NULL,
                            E_MAIL TEXT NOT NULL UNIQUE,
                            AGE INTEGER NOT NULL,
                            GENDER TEXT NOT NULL
                        );
                    ";
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the database: {ex.Message}");
                // Handle any errors appropriately (e.g., logging, user feedback)
            }
        }
        public static List<User> GetUsers()
        {
            var users = new List<User>();

            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT ID, FIRST_NAME, LAST_NAME, E_MAIL, AGE, GENDER FROM Users";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            FirstName = reader.GetString(1), // Match the property name and casing
                            LastName = reader.GetString(2), // Match the property name and casing
                            Email = reader.GetString(3), // Match the property name and casing
                            Age = reader.GetString(4), // Ensure correct data type conversion if necessary
                            Gender = reader.GetString(5)
                        });

                    }
                }
            }

            return users;
        }
    }

}

