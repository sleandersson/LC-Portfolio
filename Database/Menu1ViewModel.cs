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

        private string _currentFilePath;
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
                _currentFilePath = openFileDialog.FileName;
                await LoadCsvIntoDataTable(openFileDialog.FileName);
                DataGridVisibility = Visibility.Visible;
            }
        }

        private async Task LoadCsvIntoDataTable(string csvFilePath)
        {
            await Task.Run(() => {
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
                            SetupDataTableColumns(fields);
                            isFirstRow = false;
                        }
                        else
                        {
                            AddRowToDataTable(fields);
                        }
                    }
                }
            });
            OnPropertyChanged(nameof(DataView));
        }

        private void SetupDataTableColumns(string[] headers)
        {
            if (_dataTable.Columns.Count == 0)
            { // Only add columns if not already added
                DataColumn pkColumn = new DataColumn("PK", typeof(int))
                {
                    AutoIncrement = true,
                    AutoIncrementSeed = 1,
                    AutoIncrementStep = 1,
                    ReadOnly = true,
                    Unique = true
                };
                _dataTable.Columns.Add(pkColumn);
                foreach (string header in headers)
                {
                    _dataTable.Columns.Add(SanitizeColumnName(header), typeof(string));
                }
            }
        }

        private void AddRowToDataTable(string[] fields)
        {
            var newRow = _dataTable.NewRow();
            for (int i = 0; i < fields.Length; i++)
            {
                newRow[i + 1] = fields[i]; // i + 1 because the first column is the PK
            }
            _dataTable.Rows.Add(newRow);
        }

        private string SanitizeColumnName(string columnName)
        {
            return columnName.Replace(" ", "_").Replace(".", "_").Replace("-", "_").Replace("%", "_");
        }

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
                _currentFilePath = openFileDialog.FileName;
                await LoadDataFromDatabase(_currentFilePath);
                DataGridVisibility = Visibility.Visible;
            }
        }

        private async Task LoadDataFromDatabase(string dbFilePath)
        {
            using (var connection = new SqliteConnection($"Data Source={dbFilePath}"))
            {
                await connection.OpenAsync();
                string tableName = await GetFirstTableNameAsync(connection);

                string sql = $"SELECT * FROM {tableName};";
                using (var command = new SqliteCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    DataTable newDataTable = new DataTable();
                    newDataTable.Load(reader);
                    _dataTable.Merge(newDataTable);
                }
            }

            OnPropertyChanged(nameof(DataView));
        }

        private async Task<string> GetFirstTableNameAsync(SqliteConnection connection)
        {
            string query = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
            using (var command = new SqliteCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return reader.GetString(0);
                }
            }
            return null;
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
            return _dataTable.GetChanges() != null;
        }
        private async void ExecuteUpdateDatabase()
        {
            if (!CanUpdateDatabase())
            {
                MessageBox.Show("No changes to update.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using (var connection = new SqliteConnection($"Data Source={_currentFilePath}"))
            {
                await connection.OpenAsync();
                var transaction = connection.BeginTransaction();
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                try
                {
                    DataTable changes = _dataTable.GetChanges();
                    foreach (DataRow row in changes.Rows)
                    {
                        if (row.RowState == DataRowState.Modified)
                        {
                            // Update command goes here
                        }
                    }
                    transaction.Commit();
                    _dataTable.AcceptChanges();
                    MessageBox.Show("Database updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Failed to update database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task UpdateDatabaseAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("No file is currently loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var connection = new SqliteConnection($"Data Source={_currentFilePath}"))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    try
                    {
                        // Retrieve only modified rows
                        var changedDataTable = _dataTable.GetChanges(DataRowState.Modified);
                        if (changedDataTable == null)
                        {
                            MessageBox.Show("No changes to update.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            return; // Early exit if no changes are detected
                        }

                        foreach (DataRow row in changedDataTable.Rows)
                        {
                            string updateSql = GenerateDynamicUpdateSql(row, _dataTable.TableName);
                            command.CommandText = updateSql;

                            // Clear existing parameters
                            command.Parameters.Clear();

                            // Adding primary key parameter
                            var primaryKeyColumn = _dataTable.Columns[0].ColumnName;
                            command.Parameters.AddWithValue($"@primaryKey", row[primaryKeyColumn]);

                            // Adding parameters for each column
                            foreach (DataColumn column in _dataTable.Columns.Cast<DataColumn>().Skip(1))
                            {
                                command.Parameters.AddWithValue($"@{SanitizeColumnName(column.ColumnName)}", row[column]);
                            }

                            int result = await command.ExecuteNonQueryAsync();
                            if (result == 0)
                            {
                                throw new InvalidOperationException("No rows were updated, check your primary key and column values.");
                            }
                        }

                        transaction.Commit(); // Commit only if all updates succeed
                        _dataTable.AcceptChanges(); // Accept changes to reset row states
                        MessageBox.Show("Database updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback in case of any error
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
