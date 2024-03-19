using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;

namespace LC_Portfolio.Database
{
    public class Menu1ViewModel : INotifyPropertyChanged
    {
        public ICommand CreateDatabaseFromCsvCommand { get; private set; }
        public ICommand ViewTableCommand { get; private set; }
        private ObservableCollection<User> _users = new ObservableCollection<User>();
        private DataTable dataTable;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }

        public DataView DataView => dataTable?.DefaultView;

        public Menu1ViewModel()
        {
            CreateDatabaseFromCsvCommand = new RelayCommand(() => ImportCsvAndCreateDatabase());
            ViewTableCommand = new RelayCommand(ViewTable);
        }

        private void ImportCsvAndCreateDatabase()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string csvFilePath = openFileDialog.FileName;
                // Ensure any existing connections to the database are closed
                DatabaseConnectionManager.CloseAllConnections(); // Implement this based on your application's design

                // Now, safely delete the existing database (if it exists) and create a new one
                ProcessCsvFileAndCreateDatabase(csvFilePath);
            }
        }

        private void ProcessCsvFileAndCreateDatabase(string csvFilePath)
        {
            // Determine the path for appDatabase.db in the /bin directory
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appDatabase.db");

            // Check if appDatabase.db exists and delete it to overwrite
            if (File.Exists(databasePath))
            {
                // Before overwriting the database file
                DatabaseConnectionManager.CloseAllConnections();

                // Safe to delete or overwrite the database file now
                File.Delete(databasePath);
            }

            // Create the SQLite database and table structure based on the CSV file
            using (var connection = DatabaseConnectionManager.OpenConnection($"Data Source={databasePath}"))
            {
                connection.Open();
                using (var parser = new TextFieldParser(csvFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    if (!parser.EndOfData)
                    {
                        // Assuming the first row contains headers
                        string[] headers = parser.ReadFields();
                        string columnsDefinition = string.Join(", ", headers.Select(header => $"[{header}] TEXT"));
                        var createTableCommand = connection.CreateCommand();
                        createTableCommand.CommandText = $"CREATE TABLE IF NOT EXISTS Data ({columnsDefinition});";
                        createTableCommand.ExecuteNonQuery();

                        // Insert data from CSV into the table
                        while (!parser.EndOfData)
                        {
                            string[] fields = parser.ReadFields();
                            //string insertColumns = string.Join(", ", headers.Select(header => $"[{header}]"));
                            //string insertValues = string.Join(", ", fields.Select(field => $"@{field}"));
                            var insertCommand = connection.CreateCommand();
                            // Create parameter placeholders
                            string[] paramPlaceholders = new string[headers.Length];
                            for (int i = 0; i < headers.Length; i++)
                            {
                                string paramName = $"@param{i}";
                                paramPlaceholders[i] = paramName;
                                insertCommand.Parameters.AddWithValue(paramName, fields[i]);
                            }

                            string insertColumns = string.Join(", ", headers.Select(header => $"[{header}]"));
                            string insertValues = string.Join(", ", paramPlaceholders);

                            insertCommand.CommandText = $"INSERT INTO Data ({insertColumns}) VALUES ({insertValues});";
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Database created from CSV successfully.");
            }
        }

        private void ViewTable()
        {
            // Assuming databasePath is accessible
            string basePath = AppDomain.CurrentDomain.BaseDirectory; // Gets the base directory of the app
            string databasePath = Path.Combine(basePath); // Combines with the relative path
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)); // Ensures directory exists
            LoadDataIntoDataTable(databasePath);
        }

        private void LoadDataIntoDataTable(string databasePath)
        {
            dataTable = new DataTable();
            string fullPathToDatabase = Path.Combine(databasePath, "AppDatabase.db");
            using (var connection = DatabaseConnectionManager.OpenConnection($"Data Source={fullPathToDatabase}"))
            {
                connection.Open();
                var query = "SELECT * FROM Data";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
            OnPropertyChanged(nameof(DataView));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}