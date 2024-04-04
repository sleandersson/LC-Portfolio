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
using System.Linq;

namespace LC_Portfolio.Database
{
    public class Menu1ViewModel : INotifyPropertyChanged
    {
        public ICommand ImportCsvCommand { get; }
        public ICommand SaveDatabaseCommand { get; }
        public ICommand ConnectToDatabaseCommand { get; }
        public ICommand DeleteRowCommand { get; private set; }
        public ICommand UpdateDatabaseCommand { get; private set; }

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
            UpdateDatabaseCommand = new RelayCommand(ExecuteUpdateDatabase, CanUpdateDatabase); // Use a method to wrap async call
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
            var newDataTable = await Task.Run(() =>
            {
                var tempDataTable = new DataTable();
                DataColumn pkColumn = CreatePrimaryKeyColumn();
                tempDataTable.Columns.Add(pkColumn);

                using (var parser = new TextFieldParser(csvFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    bool isFirstRow = true;

                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();
                        if (isFirstRow)
                        {
                            AddColumnsToDataTable(fields, tempDataTable);
                            isFirstRow = false;
                        }
                        else
                        {
                            AddRowToDataTable(fields, tempDataTable);
                        }
                    }
                }
                return tempDataTable;
            });

            _dataTable = newDataTable;
            OnPropertyChanged(nameof(DataView));
        }
        private static DataColumn CreatePrimaryKeyColumn()
        {
            return new DataColumn("PK", typeof(int)) { AutoIncrement = true, AutoIncrementSeed = 1, AutoIncrementStep = 1, ReadOnly = true, Unique = true };
        }
        private static void AddColumnsToDataTable(string[] fields, DataTable dataTable)
        {
            foreach (var header in fields)
            {
                var sanitizedHeader = SanitizeColumnName(header);
                dataTable.Columns.Add(sanitizedHeader);
            }
        }
        private static void AddRowToDataTable(string[] fields, DataTable dataTable)
        {
            var newRow = dataTable.NewRow();
            for (int i = 0; i < fields.Length; i++)
            {
                newRow[i + 1] = fields[i]; // +1 to account for the PK column
            }
            dataTable.Rows.Add(newRow);
        }
        private static string SanitizeColumnName(string columnName) =>
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

                // Example connection (You might want to store this connection and use it later)
                string connectionString = $"Data Source={dbFilePath};";
                using (var connection = new SqliteConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        // Perform database operations here
                        await LoadDataFromDatabase(dbFilePath);
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

                // Dynamically get the table name if necessary
                string tableName = await GetFirstTableNameAsync(connection);

                // First, load the schema into the DataTable
                string schemaSql = $"SELECT * FROM {tableName} LIMIT 0;"; // 0 to fetch no rows, just schema
                using (var schemaCommand = new SqliteCommand(schemaSql, connection))
                using (var reader = await schemaCommand.ExecuteReaderAsync())
                {
                    dbDataTable.Load(reader);
                }

                // Then, load the actual data
                string dataSql = $"SELECT * FROM {tableName};";
                using (var dataCommand = new SqliteCommand(dataSql, connection))
                using (var reader = await dataCommand.ExecuteReaderAsync())
                {
                    // Here, we can't use dbDataTable.Load(reader) directly since it would overwrite the existing schema.
                    // Instead, manually load rows.
                    while (await reader.ReadAsync())
                    {
                        var row = dbDataTable.NewRow();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                        }
                        dbDataTable.Rows.Add(row);
                    }
                }
            }

            _dataTable = dbDataTable;
            OnPropertyChanged(nameof(DataView));
            DataGridVisibility = Visibility.Visible; // Ensure DataGrid is visible to show the loaded data
        }

        private async Task<string> GetFirstTableNameAsync(SqliteConnection connection)
        {
            string? tableName = null;
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
        private bool CanUpdateDatabase()
        {
            // Example condition: check if the DataTable has changes
            return _dataTable.GetChanges() != null;
        }
        private void ExecuteUpdateDatabase()
        {
            // Wrap the async call in a try-catch block
            try
            {
                // Call the async method without awaiting it
                var _ = UpdateDatabaseAsync(); // Discard the returned Task
            }
            catch (Exception ex)
            {
                // Handle exceptions that might occur during initiation
                MessageBox.Show($"Error updating database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task UpdateDatabaseAsync()
        {
            string dbFilePath = "Path to your database file"; // Adjust accordingly

            using (var connection = new SqliteConnection($"Data Source={dbFilePath}"))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction; // Associate the command with the transaction

                    try
                    {
                        foreach (DataRow row in _dataTable.Rows)
                        {
                            if (row.RowState == DataRowState.Modified)
                            {
                                string updateSql = GenerateDynamicUpdateSql(row, _dataTable.TableName);
                                command.CommandText = updateSql;

                                // Assuming a convention where the first column is the primary key
                                var primaryKeyColumn = _dataTable.Columns[0].ColumnName;
                                command.Parameters.AddWithValue($"@primaryKey", row[primaryKeyColumn]);

                                // Adding parameters for each column
                                foreach (DataColumn column in _dataTable.Columns.Cast<DataColumn>().Skip(1)) // Cast and then skip the primary key column
                                {
                                    command.Parameters.AddWithValue($"@{column.ColumnName}", row[column]);
                                }

                                await command.ExecuteNonQueryAsync();
                                command.Parameters.Clear(); // Clear parameters after each execution
                            }
                        }

                        transaction.Commit(); // Commit the transaction after all commands execute successfully
                        _dataTable.AcceptChanges(); // Accept changes to reset row states
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback the transaction in case of an error
                        MessageBox.Show($"Failed to update database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private string GenerateDynamicUpdateSql(DataRow row, string tableName)
        {
            // Build the SET part of the SQL command dynamically
            var setParts = new List<string>();
            foreach (DataColumn column in _dataTable.Columns)
            {
                // Assuming the first column is the primary key and should not be included in the SET part
                if (column.Ordinal > 0)
                {
                    var sanitizedColumnName = SanitizeColumnName(column.ColumnName);
                    setParts.Add($"{sanitizedColumnName} = @{sanitizedColumnName}");
                }
            }
            string setClause = string.Join(", ", setParts);

            // Assuming the first column of your table is always the primary key
            var primaryKeyColumn = SanitizeColumnName(_dataTable.Columns[0].ColumnName);

            // Building the full UPDATE command
            string updateSql = $"UPDATE {tableName} SET {setClause} WHERE {primaryKeyColumn} = @primaryKey";
            return updateSql;
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
