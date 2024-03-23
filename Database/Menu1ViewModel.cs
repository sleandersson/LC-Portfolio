using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace LC_Portfolio.Database
{
    public class Menu1ViewModel : INotifyPropertyChanged
    {
        public ICommand ImportCsvCommand { get; }
        public ICommand SaveDatabaseCommand { get; }

        private DataTable _dataTable = new DataTable();
        public DataView DataView => _dataTable.DefaultView;

        private Visibility _dataGridVisibility = Visibility.Collapsed;
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
            ImportCsvCommand = new RelayCommand(ImportCsv);
            SaveDatabaseCommand = new RelayCommand(SaveDatabase, CanSaveDatabase);
        }

        private void ImportCsv()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadCsvIntoDataTable(openFileDialog.FileName);
                DataGridVisibility = Visibility.Visible;
            }
        }
        private void LoadCsvIntoDataTable(string csvFilePath)
        {
            _dataTable.Clear();
            _dataTable.Columns.Clear();

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
                            _dataTable.Columns.Add(SanitizeColumnName(header));
                        }
                        isFirstRow = false;
                    }
                    else
                    {
                        _dataTable.Rows.Add(fields);
                    }
                }
            }
            OnPropertyChanged(nameof(DataView));
        }

        private string SanitizeColumnName(string columnName) =>
            columnName.Replace(" ", "").Replace(".", "").Replace("-", "").Replace("%", "");

        private void SaveDatabase()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite database (*.db)|*.db",
                FileName = "Data.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveDataTableToDatabase(saveFileDialog.FileName);
            }
        }

        private bool CanSaveDatabase() => _dataTable?.Rows.Count > 0;

        private void SaveDataTableToDatabase(string filePath)
        {
            string tableName = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "").Replace(".", "");
            _dataTable.TableName = tableName;

            using (var connection = new SqliteConnection($"Data Source={filePath}"))
            {
                connection.Open();
                var createTableSql = DatabaseHelper.GenerateCreateTableSql(_dataTable);
                var command = connection.CreateCommand();
                command.CommandText = createTableSql;
                command.ExecuteNonQuery();

                foreach (DataRow row in _dataTable.Rows)
                {
                    DatabaseHelper.GenerateInsertSql(_dataTable, row, command);
                    command.ExecuteNonQuery();
                }
            }
            MessageBox.Show($"Database saved successfully to {filePath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
