using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LC_Portfolio.Database
{
    public class Menu1ViewModel : INotifyPropertyChanged
    {
        public ICommand ImportCsvCommand { get; }
        public ICommand SaveDatabaseCommand { get; }
        public ICommand ConnectToDatabaseCommand { get; }
        public ICommand DeleteRowCommand { get; private set; }

        private DataTable _dataTable = new DataTable();
        public DataView DataView => _dataTable.DefaultView;

        private Visibility _dataGridVisibility = Visibility.Collapsed;

        private DataRowView _selectedRow;
        public Visibility DataGridVisibility
        {
            get => _dataGridVisibility;
            set
            {
                _dataGridVisibility = value;
                OnPropertyChanged(nameof(DataGridVisibility));
            }
        }
        public Menu1ViewModel()
        {
            ImportCsvCommand = new RelayCommand(async () => await ImportCsv());
            SaveDatabaseCommand = new RelayCommand(async () => await SaveDatabase(), CanSaveDatabase);
            ConnectToDatabaseCommand = new RelayCommand(ConnectToDatabase);
            DeleteRowCommand = new RelayCommand(DeleteSelectedRow, () => SelectedRow != null);
            _dataTable = new DataTable();
        }

        private async Task ImportCsv()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadCsvIntoDataTable(openFileDialog.FileName);
                if (DataGridVisibility != Visibility.Visible)
                {
                    DataGridVisibility = Visibility.Visible;
                }
            }
        }
        private async Task LoadCsvIntoDataTable(string csvFilePath)
        {
            DataTable newDataTable = new DataTable();
            
            await Task.Run(() =>
            {
                using (var parser = new TextFieldParser(csvFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    bool isFirstRow = true;
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (isFirstRow)
                        {
                            foreach (string header in fields)
                            {
                                newDataTable.Columns.Add(SanitizeColumnName(header));
                            }
                            isFirstRow = false;
                        }
                        else
                        {
                            newDataTable.Rows.Add(fields);
                        }
                    }
                }
                });
            _dataTable = newDataTable;
            OnPropertyChanged(nameof(DataView));
        }

        private string SanitizeColumnName(string columnName) =>
            columnName.Replace(" ", "").Replace(".", "").Replace("-", "").Replace("%", "");

        private async Task SaveDatabase()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite database (*.db)|*.db",
                FileName = "LocalDatabase.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await SaveDataTableToDatabase(saveFileDialog.FileName);
            }
        }

        private bool CanSaveDatabase() => _dataTable != null && _dataTable.Rows.Count > 0;

        private async Task SaveDataTableToDatabase(string filePath)
        {
            string tableName = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "").Replace(".", "");
            _dataTable.TableName = tableName;

            using (var connection = new SqliteConnection($"Data Source={filePath}"))
            {
                await connection.OpenAsync();
                var createTableSql = DatabaseHelper.GenerateCreateTableSql(_dataTable);
                var command = connection.CreateCommand();
                command.CommandText = createTableSql;
                await command.ExecuteNonQueryAsync();

                foreach (DataRow row in _dataTable.Rows)
                {
                    DatabaseHelper.GenerateInsertSql(_dataTable, row, command);
                    await command.ExecuteNonQueryAsync();
                }
            }
            MessageBox.Show($"Database saved successfully to {filePath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private async void ConnectToDatabase()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite database (*.db)|*.db",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string dbFilePath = openFileDialog.FileName;
                // Now you have the path to the .db file, you can establish a connection
                // For demonstration, let's just show the path in a MessageBox
                //MessageBox.Show($"Connecting to database at: {dbFilePath}", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Information);

                // Example connection (You might want to store this connection and use it later)
                string connectionString = $"Data Source={dbFilePath};";
                using (var connection = new SqliteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // Perform database operations here
                        await LoadDataFromDatabase(dbFilePath);
                        //MessageBox.Show("Connected successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private async Task LoadDataFromDatabase(string dbFilePath)
        {
            DataTable dbDataTable = new DataTable();

            using (var connection = new SqliteConnection($"Data Source={dbFilePath}"))
            {
                await connection.OpenAsync();

                // Here you can dynamically get the table name if needed
                string tableName = await GetFirstTableNameAsync(connection);

                string query = $"SELECT * FROM {tableName};";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    dbDataTable.Load(reader);
                }
            }
            _dataTable = dbDataTable;
            OnPropertyChanged(nameof(DataView));
            DataGridVisibility = Visibility.Visible; // Make sure DataGrid is visible to show the loaded data
        }

        private async Task<string> GetFirstTableNameAsync(SqliteConnection connection)
        {
            string tableName = null;
            // Query to get the first table name
            string query = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
            using (var command = new SqliteCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    tableName = reader.GetString(0);
                }
            }
            return tableName;
        }
        public DataRowView SelectedRow
        {
            get => _selectedRow;
            set
            {
                _selectedRow = value;
                OnPropertyChanged(nameof(SelectedRow));
            }
        }
        private void DeleteSelectedRow()
        {
            if (_selectedRow != null)
            {
                _dataTable.Rows.Remove(_selectedRow.Row);
                OnPropertyChanged(nameof(DataView)); // Refresh the DataView
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
