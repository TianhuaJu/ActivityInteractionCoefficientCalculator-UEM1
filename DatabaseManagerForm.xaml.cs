using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    public partial class DatabaseManagerWindow : Window
    {
        private readonly string basicDbPath = "Data Source=data\\BasicData.db";
        private readonly string myDbPath = "Data Source=data\\myDB.db";
        private DataTable basicDataTable;
        private DataTable myDataTable;
        private string currentExperimentTable = "first_order";

        public DatabaseManagerWindow()
        {
            InitializeComponent();
            this.Loaded += DatabaseManagerForm_Load;
        }

        private async void DatabaseManagerForm_Load(object sender, RoutedEventArgs e)
        {
            await LoadBasicDataAsync();
            await LoadExperimentTablesAsync();
            await LoadExperimentDataAsync();
        }

        #region Miedema参数数据库操作

        private async Task LoadBasicDataAsync(string filter = "")
        {
            UpdateStatus("正在加载 Miedema 数据...");
            toolbar1.IsEnabled = false; // Disable toolbar during load

            try
            {
                basicDataTable = await Task.Run(() =>
                {
                    var dt = new DataTable();
                    using (var connection = new SQLiteConnection(basicDbPath))
                    {
                        connection.Open();
                        string query = "SELECT * FROM Miedema1988";
                        if (!string.IsNullOrWhiteSpace(filter))
                        {
                            query += " WHERE Symbol LIKE @filter";
                        }

                        var command = new SQLiteCommand(query, connection);
                        if (!string.IsNullOrWhiteSpace(filter))
                        {
                            command.Parameters.AddWithValue("@filter", $"%{filter}%");
                        }

                        // 使用 DataReader 逐行读取，以清洗数据
                        using (var reader = command.ExecuteReader())
                        {
                            // 根据 Reader 的结构创建 DataTable 的列
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                // 使用 reader.GetFieldType() 来获取更准确的列类型
                                dt.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                            }

                            // 逐行读取并清洗数据
                            while (reader.Read())
                            {
                                DataRow row = dt.NewRow();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        row[i] = DBNull.Value;
                                    }
                                    else
                                    {
                                        // 检查是否存在无效字符串，如果存在则视为空值
                                        string valueStr = reader.GetValue(i).ToString();
                                        if (string.IsNullOrWhiteSpace(valueStr) || valueStr == "<>" || valueStr == "N/A" || valueStr == "-")
                                        {
                                            row[i] = DBNull.Value;
                                        }
                                        else
                                        {
                                            // 尝试转换回原始类型
                                            try
                                            {
                                                row[i] = Convert.ChangeType(valueStr, dt.Columns[i].DataType);
                                            }
                                            catch
                                            {
                                                // 如果转换失败（例如isTrans列中的<>），也视为空值
                                                row[i] = DBNull.Value;
                                            }
                                        }
                                    }
                                }
                                dt.Rows.Add(row);
                            }
                        }

                        if (dt.Columns.Contains("Symbol"))
                        {
                            try
                            {
                                dt.PrimaryKey = new DataColumn[] { dt.Columns["Symbol"] };
                            }
                            catch (Exception) { /* Ignore if PK cannot be set */ }
                        }
                    }
                    return dt;
                });

                dataGridView1.ItemsSource = basicDataTable.DefaultView;
                UpdateStatus($"Miedema参数数据库: 共 {basicDataTable.Rows.Count} 条记录");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载Miedema数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("加载 Miedema 数据失败");
            }
            finally
            {
                toolbar1.IsEnabled = true;
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            await LoadBasicDataAsync();
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await LoadBasicDataAsync(txtSearch.Text.Trim());
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure any pending edits are committed to the DataTable
                dataGridView1.CommitEdit(DataGridEditingUnit.Row, true);

                DataTable changes = basicDataTable.GetChanges();
                if (changes == null || changes.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show("没有检测到需要保存的修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // --- Validation specific to Miedema table ---
                if (!ValidateSymbolUniqueness(changes, out List<string> duplicateSymbols))
                {
                    string errorMsg = $"检测到重复或空的 Symbol，无法保存! \n\n重复项: {string.Join(", ", duplicateSymbols)}\n\nSymbol 必须是唯一的，请修改后再保存。";
                    System.Windows.MessageBox.Show(errorMsg, "Symbol 重复错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // --- Standard Save Logic using CommandBuilder ---
                using (var connection = new SQLiteConnection(basicDbPath))
                {
                    connection.Open();
                    var adapter = new SQLiteDataAdapter("SELECT * FROM Miedema1988", connection);
                    var commandBuilder = new SQLiteCommandBuilder(adapter);

                    int rowsAffected = adapter.Update(basicDataTable);
                    System.Windows.MessageBox.Show($"✅ 数据保存成功!\n\n更新了 {rowsAffected} 行", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus($"数据已保存 ({rowsAffected} 行受影响)");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存数据失败: {ex.Message}\n\n可能原因: 违反了数据库约束(如Symbol重复)。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // Optional: You might want to reload the data to discard failed changes
                // LoadBasicDataAsync(); 
            }
        }

        private bool ValidateSymbolUniqueness(DataTable changes, out List<string> duplicateSymbols)
        {
            duplicateSymbols = new List<string>();
            var allSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Load all existing, unchanged symbols
            foreach (DataRow row in basicDataTable.Rows)
            {
                if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Added)
                {
                    // For modified rows, add the original value to check against changes
                    var version = row.RowState == DataRowState.Modified ? DataRowVersion.Original : DataRowVersion.Current;
                    if (row.HasVersion(version) && row["Symbol", version] != DBNull.Value)
                        allSymbols.Add(row["Symbol", version].ToString());
                }
            }

            // 2. Check new and modified symbols in the changes for uniqueness
            foreach (DataRow row in changes.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                if (row["Symbol"] == DBNull.Value || string.IsNullOrWhiteSpace(row["Symbol"].ToString()))
                {
                    duplicateSymbols.Add("[空值]");
                    continue;
                }

                string newSymbol = row["Symbol"].ToString();
                if (!allSymbols.Add(newSymbol)) // .Add returns false if item already exists
                {
                    duplicateSymbols.Add(newSymbol);
                }
            }
            return duplicateSymbols.Count == 0;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridView1.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("请先选择要删除的行", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show($"确定要删除选中的 {dataGridView1.SelectedItems.Count} 行吗？\n删除后需要点击“保存修改”按钮以生效。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var rowsToDelete = dataGridView1.SelectedItems.Cast<DataRowView>().ToList();
                    foreach (var rowView in rowsToDelete)
                    {
                        rowView.Row.Delete();
                    }
                    UpdateStatus($"已标记 {rowsToDelete.Count} 行待删除。请点击“保存修改”来确认。");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"标记删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region 实验数据库操作

        private async Task LoadExperimentTablesAsync()
        {
            UpdateStatus("正在加载表列表...");
            tableSelectorPanel.IsEnabled = false;
            try
            {
                List<string> tables = await Task.Run(() =>
                {
                    var tableList = new List<string>();
                    using (var connection = new SQLiteConnection(myDbPath))
                    {
                        connection.Open();
                        var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name", connection);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tableList.Add(reader.GetString(0));
                            }
                        }
                    }
                    return tableList;
                });

                cmbTables.ItemsSource = tables;
                if (tables.Contains(currentExperimentTable))
                {
                    cmbTables.SelectedItem = currentExperimentTable;
                }
                else if (tables.Count > 0)
                {
                    cmbTables.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载表列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                tableSelectorPanel.IsEnabled = true;
            }
        }

        private async void cmbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTables.SelectedItem == null || e.AddedItems.Count == 0) return;

            string newTable = e.AddedItems[0].ToString();
            if (newTable == currentExperimentTable) return;

            if (myDataTable != null && myDataTable.GetChanges() != null)
            {
                var result = System.Windows.MessageBox.Show("当前表有未保存的修改，切换表会丢失这些修改。\n\n是否继续？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    cmbTables.SelectedItem = currentExperimentTable;
                    return;
                }
            }

            currentExperimentTable = newTable;
            await LoadExperimentDataAsync();
        }

        private async void btnRefreshTables_Click(object sender, RoutedEventArgs e)
        {
            await LoadExperimentTablesAsync();
            System.Windows.MessageBox.Show("? 表列表已刷新!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<string> GetPrimaryKeyColumnAsync(string tableName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var connection = new SQLiteConnection(myDbPath))
                    {
                        connection.Open();
                        var cmd = new SQLiteCommand($"PRAGMA table_info('{tableName}')", connection);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetInt32(5) > 0)
                                    return reader.GetString(1);
                            }
                        }
                    }
                }
                catch { }
                return null;
            });
        }

        private async Task LoadExperimentDataAsync(string filter = "")
        {
            if (string.IsNullOrEmpty(currentExperimentTable)) return;

            UpdateStatus($"正在加载 [{currentExperimentTable}] 数据...");
            toolbar2.IsEnabled = false;

            try
            {
                string primaryKey = await GetPrimaryKeyColumnAsync(currentExperimentTable);

                myDataTable = await Task.Run(() =>
                {
                    var dt = new DataTable();
                    using (var connection = new SQLiteConnection(myDbPath))
                    {
                        connection.Open();
                        string query = $"SELECT * FROM {currentExperimentTable}";

                        if (!string.IsNullOrWhiteSpace(filter) && currentExperimentTable == "first_order")
                        {
                            query += " WHERE solv LIKE @filter OR solui LIKE @filter OR soluj LIKE @filter";
                        }

                        var command = new SQLiteCommand(query, connection);
                        if (!string.IsNullOrWhiteSpace(filter))
                        {
                            command.Parameters.AddWithValue("@filter", $"%{filter}%");
                        }

                        var adapter = new SQLiteDataAdapter(command);
                        adapter.Fill(dt);

                        if (!string.IsNullOrEmpty(primaryKey) && dt.Columns.Contains(primaryKey))
                        {
                            try { dt.PrimaryKey = new DataColumn[] { dt.Columns[primaryKey] }; } catch { }
                        }
                    }
                    return dt;
                });

                dataGridView2.ItemsSource = myDataTable.DefaultView;
                string pkInfo = myDataTable.PrimaryKey.Length > 0 ? $" (主键: {myDataTable.PrimaryKey[0].ColumnName})" : " (警告: 无主键)";
                UpdateStatus($"实验数据库 [{currentExperimentTable}]: 共 {myDataTable.Rows.Count} 条记录{pkInfo}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载实验数据失败: {ex.Message}\n\n表名: {currentExperimentTable}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"加载 [{currentExperimentTable}] 失败");
            }
            finally
            {
                toolbar2.IsEnabled = true;
            }
        }

        private async void btnRefresh2_Click(object sender, RoutedEventArgs e)
        {
            txtSearch2.Clear();
            await LoadExperimentDataAsync();
        }

        private async void btnSearch2_Click(object sender, RoutedEventArgs e)
        {
            await LoadExperimentDataAsync(txtSearch2.Text.Trim());
        }

        private void btnSave2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // *** FIX 1: Force pending edits to commit ***
                ForceEndEdit(dataGridView2);

                DataTable changes = myDataTable.GetChanges();

                if (changes == null || changes.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show("没有检测到需要保存的修改!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (myDataTable.PrimaryKey.Length == 0)
                {
                    System.Windows.MessageBox.Show($"警告: 表 '{currentExperimentTable}' 没有主键，可能导致更新失败。", "主键警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                using (var connection = new SQLiteConnection(myDbPath))
                {
                    connection.Open();
                    var adapter = new SQLiteDataAdapter($"SELECT * FROM {currentExperimentTable}", connection);
                    var commandBuilder = new SQLiteCommandBuilder(adapter);

                    int rowsAffected = adapter.Update(myDataTable);
                    System.Windows.MessageBox.Show($"? 数据保存成功!\n\n更新了 {rowsAffected} 行", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatus($"数据已保存 ({rowsAffected} 行受影响)");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败: {ex.Message}\n\n可能原因: 违反了唯一性约束或数据类型不匹配。", "保存错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAdd2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myDataTable == null) return;

                DataRow newRow = myDataTable.NewRow();

                // *** FIX 2: Calculate and assign a new Primary Key before adding the row ***
                string pkColumnName = "No."; // Column name from the error message
                if (myDataTable.Columns.Contains(pkColumnName))
                {
                    int maxId = 0;
                    foreach (DataRow row in myDataTable.Rows)
                    {
                        // Exclude already deleted rows from max ID calculation
                        if (row.RowState != DataRowState.Deleted && row[pkColumnName] != DBNull.Value)
                        {
                            if (int.TryParse(row[pkColumnName].ToString(), out int currentId))
                            {
                                if (currentId > maxId)
                                {
                                    maxId = currentId;
                                }
                            }
                        }
                    }
                    newRow[pkColumnName] = maxId + 1;
                }

                myDataTable.Rows.Add(newRow);

                dataGridView2.ScrollIntoView(newRow);
                dataGridView2.SelectedItem = newRow;
                UpdateStatus("已添加新行，请填写数据后保存");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加新行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete2_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridView2.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("请先选择要删除的行", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show($"确定要删除选中的 {dataGridView2.SelectedItems.Count} 行吗？\n删除后需要点击“保存修改”按钮以生效。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var rowsToDelete = dataGridView2.SelectedItems.Cast<DataRowView>().ToList();
                    foreach (var rowView in rowsToDelete)
                    {
                        rowView.Row.Delete();
                    }
                    UpdateStatus($"已标记 {rowsToDelete.Count} 行待删除。请点击“保存修改”来确认。");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"标记删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnExport2_Click(object sender, RoutedEventArgs e)
        {
            if (myDataTable == null || myDataTable.Rows.Count == 0)
            {
                System.Windows.MessageBox.Show("没有数据可以导出!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                FileName = $"{currentExperimentTable}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    var headers = myDataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                    sb.AppendLine(string.Join(",", headers));

                    foreach (DataRow row in myDataTable.Rows)
                    {
                        var values = row.ItemArray.Select(v =>
                        {
                            string value = v?.ToString() ?? "";
                            if (value.Contains(',') || value.Contains('\"'))
                            {
                                return $"\"{value.Replace("\"", "\"\"")}\"";
                            }
                            return value;
                        });
                        sb.AppendLine(string.Join(",", values));
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    System.Windows.MessageBox.Show($"? 导出成功!\n\n文件: {saveDialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Helper Methods
        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() => {
                statusLabel.Text = message;
                statusLabel2.Text = message;
            });
        }

        /// <summary>
        /// Helper method to force a DataGrid to commit any pending edits.
        /// </summary>
        private void ForceEndEdit(DataGrid grid)
        {
            if (grid == null) return;

            // 这一行非常重要，它取消任何单元格级别的编辑
            grid.CancelEdit(DataGridEditingUnit.Cell);
            // 接下来，提交行级别的编辑
            grid.CommitEdit(DataGridEditingUnit.Row, true);

            // 然后，我们通过操作底层数据视图来强制提交挂起的事务
            // 这是解决此问题的最可靠方法
            if (grid.Items is IEditableCollectionView editableView)
            {
                if (editableView.IsAddingNew)
                {
                    editableView.CommitNew();
                }
                else if (editableView.IsEditingItem)
                {
                    editableView.CommitEdit();
                }
            }
        }
        #endregion
    }
}