using System;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
// ❌ 不要添加: using System.Windows.Forms;

namespace Activity_Interaction_Coefficient_Calculator_UEM1
{
    public partial class DatabaseManagerWindow : Window
    {
        private readonly string basicDbPath = "Data Source=data\\BasicData.db";
        private readonly string myDbPath = "Data Source=data\\myDB.db";
        private DataTable basicDataTable;
        private DataTable myDataTable;
        private string currentExperimentTable = "first_order";

        /// <summary>
        /// 统一更新状态标签
        /// </summary>
        private void UpdateStatus(string message)
        {
            statusLabel.Text = message;

            if (this.FindName("statusLabel2") is System.Windows.Controls.TextBlock statusLabel2)
            {
                statusLabel2.Text = message;
            }
        }

        public DatabaseManagerWindow()
        {
            InitializeComponent();
            this.Loaded += DatabaseManagerForm_Load;
        }

        private void DatabaseManagerForm_Load(object sender, RoutedEventArgs e)
        {
            LoadBasicData();
            LoadExperimentTables();
            LoadExperimentData();
        }

        #region Miedema参数数据库操作

        private void LoadBasicData(string filter = "")
        {
            try
            {
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

                    using (var reader = command.ExecuteReader())
                    {
                        basicDataTable = new DataTable();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            basicDataTable.Columns.Add(reader.GetName(i), typeof(string));
                        }

                        while (reader.Read())
                        {
                            DataRow row = basicDataTable.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    string value = reader.GetValue(i).ToString();
                                    if (string.IsNullOrWhiteSpace(value) || value == "<>" || value == "N/A" || value == "-")
                                    {
                                        row[i] = DBNull.Value;
                                    }
                                    else
                                    {
                                        row[i] = value;
                                    }
                                }
                                else
                                {
                                    row[i] = DBNull.Value;
                                }
                            }
                            basicDataTable.Rows.Add(row);
                        }
                    }

                    try
                    {
                        if (basicDataTable.Columns.Contains("Symbol"))
                        {
                            basicDataTable.PrimaryKey = new DataColumn[] { basicDataTable.Columns["Symbol"] };
                        }
                    }
                    catch (Exception) { }

                    basicDataTable.AcceptChanges();
                    dataGridView1.ItemsSource = basicDataTable.DefaultView;
                    UpdateStatus($"Miedema参数数据库: 共 {basicDataTable.Rows.Count} 条记录");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载Miedema数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBasicData();
            if (this.FindName("txtSearch") is System.Windows.Controls.TextBox txtSearch)
            {
                txtSearch.Clear();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("txtSearch") is System.Windows.Controls.TextBox txtSearch)
            {
                LoadBasicData(txtSearch.Text.Trim());
            }
        }

        private void ForceEndEdit(System.Windows.Controls.DataGrid dataGrid)
        {
            try
            {
                if (dataGrid.CurrentCell != null)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                }

                var selectedItems = dataGrid.SelectedItems.Cast<object>().ToList();
                dataGrid.UnselectAll();
                statusLabel.Focus();
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

                foreach (var item in selectedItems)
                {
                    dataGrid.SelectedItems.Add(item);
                }
            }
            catch { }
        }

        private bool ValidateSymbolUniqueness(DataTable changes, out List<string> duplicateSymbols)
        {
            duplicateSymbols = new List<string>();

            if (!basicDataTable.Columns.Contains("Symbol"))
                return true;

            var existingSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in basicDataTable.Rows)
            {
                if (row.RowState == DataRowState.Unchanged && row["Symbol"] != DBNull.Value)
                {
                    string symbol = row["Symbol"].ToString();
                    if (!string.IsNullOrWhiteSpace(symbol))
                    {
                        existingSymbols.Add(symbol.Trim());
                    }
                }
            }

            foreach (DataRow row in changes.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                if (row["Symbol"] == DBNull.Value || string.IsNullOrWhiteSpace(row["Symbol"].ToString()))
                {
                    duplicateSymbols.Add("[空值]");
                    continue;
                }

                string newSymbol = row["Symbol"].ToString().Trim();

                if (row.RowState == DataRowState.Added)
                {
                    if (existingSymbols.Contains(newSymbol))
                    {
                        duplicateSymbols.Add(newSymbol);
                    }
                    else
                    {
                        existingSymbols.Add(newSymbol);
                    }
                }
                else if (row.RowState == DataRowState.Modified)
                {
                    string originalSymbol = row["Symbol", DataRowVersion.Original].ToString().Trim();

                    if (!newSymbol.Equals(originalSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        if (existingSymbols.Contains(newSymbol))
                        {
                            duplicateSymbols.Add($"{newSymbol} (原: {originalSymbol})");
                        }
                        else
                        {
                            existingSymbols.Remove(originalSymbol);
                            existingSymbols.Add(newSymbol);
                        }
                    }
                }
            }

            var changesSymbols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in changes.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;

                if (row["Symbol"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["Symbol"].ToString()))
                {
                    string symbol = row["Symbol"].ToString().Trim();
                    if (changesSymbols.ContainsKey(symbol))
                    {
                        changesSymbols[symbol]++;
                    }
                    else
                    {
                        changesSymbols[symbol] = 1;
                    }
                }
            }

            foreach (var kvp in changesSymbols.Where(x => x.Value > 1))
            {
                if (!duplicateSymbols.Contains(kvp.Key))
                {
                    duplicateSymbols.Add($"{kvp.Key} (在本次变更中重复 {kvp.Value} 次)");
                }
            }

            return duplicateSymbols.Count == 0;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ForceEndEdit(dataGridView1);

                DataTable changes = basicDataTable.GetChanges();
                if (changes == null || changes.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "没有检测到需要保存的修改!\n\n" +
                        "提示:\n" +
                        "• 如果您刚刚修改了数据，请确保:\n" +
                        "  1. 修改后按 Enter 键或点击其他单元格\n" +
                        "  2. 不要只是选中空行\n" +
                        "  3. 确保实际输入了内容\n" +
                        "• 如果问题持续，请尝试:\n" +
                        "  1. 修改数据\n" +
                        "  2. 点击其他地方\n" +
                        "  3. 等待1秒\n" +
                        "  4. 再点击保存",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                List<string> duplicateSymbols;
                if (!ValidateSymbolUniqueness(changes, out duplicateSymbols))
                {
                    var message = new StringBuilder();
                    message.AppendLine("❌ 检测到重复的 Symbol，无法保存!");
                    message.AppendLine();
                    message.AppendLine("重复的 Symbol 列表:");
                    foreach (var symbol in duplicateSymbols)
                    {
                        message.AppendLine($"  • {symbol}");
                    }
                    message.AppendLine();
                    message.AppendLine("提示: Symbol 必须是唯一的，请修改后再保存。");

                    System.Windows.MessageBox.Show(
                        message.ToString(),
                        "Symbol 重复错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                bool hasEmptySymbol = false;
                foreach (DataRow row in changes.Rows)
                {
                    if (row.RowState != DataRowState.Deleted)
                    {
                        if (row["Symbol"] == DBNull.Value || string.IsNullOrWhiteSpace(row["Symbol"].ToString()))
                        {
                            hasEmptySymbol = true;
                            break;
                        }
                    }
                }

                if (hasEmptySymbol)
                {
                    System.Windows.MessageBox.Show(
                        "❌ 检测到空的 Symbol 值!\n\nSymbol 是主键，不能为空。\n请填写所有 Symbol 后再保存。",
                        "Symbol 为空错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                using (var connection = new SQLiteConnection(basicDbPath))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (DataRow row in changes.Rows)
                            {
                                if (row.RowState == DataRowState.Modified)
                                {
                                    var updateCmd = BuildUpdateCommand(connection, row);
                                    updateCmd.ExecuteNonQuery();
                                }
                                else if (row.RowState == DataRowState.Added)
                                {
                                    var insertCmd = BuildInsertCommand(connection, row);
                                    insertCmd.ExecuteNonQuery();
                                }
                                else if (row.RowState == DataRowState.Deleted)
                                {
                                    string symbol = row["Symbol", DataRowVersion.Original].ToString();
                                    if (!string.IsNullOrEmpty(symbol))
                                    {
                                        var deleteCmd = new SQLiteCommand("DELETE FROM Miedema1988 WHERE Symbol = @Symbol", connection);
                                        deleteCmd.Parameters.AddWithValue("@Symbol", symbol);
                                        deleteCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            transaction.Commit();
                            basicDataTable.AcceptChanges();

                            System.Windows.MessageBox.Show("✅ 数据保存成功!", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            UpdateStatus("数据已保存");
                        }
                        catch (SQLiteException sqlEx)
                        {
                            transaction.Rollback();

                            if (sqlEx.Message.Contains("UNIQUE") || sqlEx.Message.Contains("constraint"))
                            {
                                System.Windows.MessageBox.Show(
                                    $"❌ 数据库约束错误: Symbol 重复!\n\n{sqlEx.Message}\n\n提示: 可能是数据库中已存在相同的 Symbol。",
                                    "数据库错误",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存数据失败: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private SQLiteCommand BuildUpdateCommand(SQLiteConnection connection, DataRow row)
        {
            var command = connection.CreateCommand();
            var sb = new StringBuilder("UPDATE Miedema1988 SET ");

            bool first = true;
            foreach (DataColumn col in basicDataTable.Columns)
            {
                if (col.ColumnName.Equals("Symbol", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!first)
                    sb.Append(", ");

                sb.Append($"{col.ColumnName} = @{col.ColumnName}");
                first = false;
            }

            sb.Append(" WHERE Symbol = @Symbol");
            command.CommandText = sb.ToString();

            foreach (DataColumn col in basicDataTable.Columns)
            {
                object value = row[col.ColumnName];
                command.Parameters.AddWithValue($"@{col.ColumnName}",
                    value == DBNull.Value ? (object)DBNull.Value : value);
            }

            return command;
        }

        private SQLiteCommand BuildInsertCommand(SQLiteConnection connection, DataRow row)
        {
            var command = connection.CreateCommand();
            var sbColumns = new StringBuilder();
            var sbValues = new StringBuilder();

            bool first = true;
            foreach (DataColumn col in basicDataTable.Columns)
            {
                if (!first)
                {
                    sbColumns.Append(", ");
                    sbValues.Append(", ");
                }

                sbColumns.Append(col.ColumnName);
                sbValues.Append($"@{col.ColumnName}");
                first = false;
            }

            command.CommandText = $"INSERT INTO Miedema1988 ({sbColumns}) VALUES ({sbValues})";

            foreach (DataColumn col in basicDataTable.Columns)
            {
                object value = row[col.ColumnName];
                command.Parameters.AddWithValue($"@{col.ColumnName}",
                    value == DBNull.Value ? (object)DBNull.Value : value);
            }

            return command;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridView1.SelectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("请先选择要删除的行", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show("确定要删除选中的行吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = new SQLiteConnection(basicDbPath))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                foreach (var item in dataGridView1.SelectedItems)
                                {
                                    if (item is DataRowView rowView)
                                    {
                                        string symbol = rowView.Row["Symbol"].ToString();
                                        if (!string.IsNullOrEmpty(symbol))
                                        {
                                            var cmd = new SQLiteCommand("DELETE FROM Miedema1988 WHERE Symbol = @Symbol", connection);
                                            cmd.Parameters.AddWithValue("@Symbol", symbol);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                transaction.Commit();
                                System.Windows.MessageBox.Show("✅ 删除成功!", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadBasicData();
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region 实验数据库操作

        private void LoadExperimentTables()
        {
            try
            {
                using (var connection = new SQLiteConnection(myDbPath))
                {
                    connection.Open();
                    var tables = new List<string>();

                    var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            if (!tableName.StartsWith("sqlite_"))
                            {
                                tables.Add(tableName);
                            }
                        }
                    }

                    if (this.FindName("cmbTables") is System.Windows.Controls.ComboBox cmbTables)
                    {
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
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载表列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox cmb && cmb.SelectedItem != null)
            {
                string newTable = cmb.SelectedItem.ToString();
                if (newTable != currentExperimentTable)
                {
                    if (myDataTable != null && myDataTable.GetChanges() != null)
                    {
                        var result = System.Windows.MessageBox.Show(
                            "当前表有未保存的修改，切换表会丢失这些修改。\n\n是否继续？",
                            "警告",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                        {
                            cmb.SelectedItem = currentExperimentTable;
                            return;
                        }
                    }

                    currentExperimentTable = newTable;
                    LoadExperimentData();
                }
            }
        }

        private void btnRefreshTables_Click(object sender, RoutedEventArgs e)
        {
            LoadExperimentTables();
            System.Windows.MessageBox.Show("✅ 表列表已刷新!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetPrimaryKeyColumn(string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection(myDbPath))
                {
                    connection.Open();
                    var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool isPrimaryKey = reader.GetInt32(5) > 0;
                            if (isPrimaryKey)
                            {
                                return reader.GetString(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"获取主键失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        private void LoadExperimentData(string filter = "")
        {
            try
            {
                using (var connection = new SQLiteConnection(myDbPath))
                {
                    connection.Open();
                    string query = $"SELECT * FROM {currentExperimentTable}";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        if (currentExperimentTable == "first_order")
                        {
                            query += " WHERE solv LIKE @filter OR solui LIKE @filter OR soluj LIKE @filter OR CAST([No.] AS TEXT) LIKE @filter";
                        }
                        else
                        {
                            query += " WHERE 1=0";
                        }
                    }

                    var command = new SQLiteCommand(query, connection);
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        command.Parameters.AddWithValue("@filter", $"%{filter}%");
                    }

                    var adapter = new SQLiteDataAdapter(command);
                    myDataTable = new DataTable();
                    adapter.Fill(myDataTable);

                    try
                    {
                        string primaryKey = GetPrimaryKeyColumn(currentExperimentTable);
                        if (!string.IsNullOrEmpty(primaryKey) && myDataTable.Columns.Contains(primaryKey))
                        {
                            myDataTable.PrimaryKey = new DataColumn[] { myDataTable.Columns[primaryKey] };
                        }
                    }
                    catch (Exception) { }

                    myDataTable.AcceptChanges();
                    dataGridView2.ItemsSource = myDataTable.DefaultView;

                    string pkInfo = "";
                    if (myDataTable.PrimaryKey.Length > 0)
                    {
                        pkInfo = $" (主键: {myDataTable.PrimaryKey[0].ColumnName})";
                    }

                    UpdateStatus($"实验数据库 [{currentExperimentTable}]: 共 {myDataTable.Rows.Count} 条记录{pkInfo}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载实验数据失败: {ex.Message}\n\n表名: {currentExperimentTable}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh2_Click(object sender, RoutedEventArgs e)
        {
            LoadExperimentData();
            if (this.FindName("txtSearch2") is System.Windows.Controls.TextBox txtSearch2)
            {
                txtSearch2.Clear();
            }
        }

        private void btnSearch2_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindName("txtSearch2") is System.Windows.Controls.TextBox txtSearch2)
            {
                LoadExperimentData(txtSearch2.Text.Trim());
            }
        }

        private void btnSave2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ForceEndEdit(dataGridView2);

                DataTable changes = myDataTable.GetChanges();

                if (changes == null || changes.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        $"没有检测到需要保存的修改!\n\n" +
                        $"提示:\n" +
                        $"• 修改单元格后，请按 Enter 键或点击其他单元格\n" +
                        $"• 不要只是选中空行\n" +
                        $"• 如果问题持续，尝试:\n" +
                        $"  1. 修改数据\n" +
                        $"  2. 点击其他地方\n" +
                        $"  3. 等待1秒\n" +
                        $"  4. 再点击保存",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (myDataTable.PrimaryKey.Length == 0)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"警告: 表 '{currentExperimentTable}' 没有设置主键!\n\n" +
                        $"没有主键可能导致更新失败。建议:\n" +
                        $"1. 为表添加主键列（如 No. 或 id）\n" +
                        $"2. 使用 SQLite 工具修改表结构\n\n" +
                        $"是否仍要尝试保存？",
                        "主键警告",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                using (var connection = new SQLiteConnection(myDbPath))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int rowsAffected = 0;
                            string pkColumn = myDataTable.PrimaryKey.Length > 0 ? myDataTable.PrimaryKey[0].ColumnName : null;

                            foreach (DataRow row in changes.Rows)
                            {
                                if (row.RowState == DataRowState.Modified)
                                {
                                    var updateCmd = BuildExperimentUpdateCommand(connection, row, pkColumn);
                                    rowsAffected += updateCmd.ExecuteNonQuery();
                                }
                                else if (row.RowState == DataRowState.Added)
                                {
                                    var insertCmd = BuildExperimentInsertCommand(connection, row);
                                    rowsAffected += insertCmd.ExecuteNonQuery();
                                }
                                else if (row.RowState == DataRowState.Deleted)
                                {
                                    if (!string.IsNullOrEmpty(pkColumn))
                                    {
                                        var pkValue = row[pkColumn, DataRowVersion.Original];
                                        var deleteCmd = new SQLiteCommand($"DELETE FROM [{currentExperimentTable}] WHERE [{pkColumn}] = @pkValue", connection);
                                        deleteCmd.Parameters.AddWithValue("@pkValue", pkValue);
                                        rowsAffected += deleteCmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            myDataTable.AcceptChanges();

                            System.Windows.MessageBox.Show(
                                $"✅ 数据保存成功!\n\n更新了 {rowsAffected} 行",
                                "成功",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            UpdateStatus($"数据已保存 ({rowsAffected} 行受影响)");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Windows.MessageBox.Show(
                                $"保存失败: {ex.Message}\n\n" +
                                $"可能的原因:\n" +
                                $"• 表 '{currentExperimentTable}' 缺少主键\n" +
                                $"• 违反了唯一性约束\n" +
                                $"• 数据类型不匹配\n\n" +
                                $"建议: 确保表有主键，并检查数据的有效性。",
                                "保存错误",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"保存数据失败: {ex.Message}\n\n详细信息:\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private SQLiteCommand BuildExperimentUpdateCommand(SQLiteConnection connection, DataRow row, string pkColumn)
        {
            var command = connection.CreateCommand();
            var sb = new StringBuilder($"UPDATE [{currentExperimentTable}] SET ");

            bool first = true;
            foreach (DataColumn col in myDataTable.Columns)
            {
                // 跳过主键列
                if (!string.IsNullOrEmpty(pkColumn) && col.ColumnName.Equals(pkColumn, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!first)
                    sb.Append(", ");

                sb.Append($"[{col.ColumnName}] = @{col.ColumnName.Replace(".", "_")}");
                first = false;
            }

            if (!string.IsNullOrEmpty(pkColumn))
            {
                sb.Append($" WHERE [{pkColumn}] = @pk_value");
            }

            command.CommandText = sb.ToString();

            // 添加参数
            foreach (DataColumn col in myDataTable.Columns)
            {
                if (!string.IsNullOrEmpty(pkColumn) && col.ColumnName.Equals(pkColumn, StringComparison.OrdinalIgnoreCase))
                    continue;

                object value = row[col.ColumnName];
                command.Parameters.AddWithValue($"@{col.ColumnName.Replace(".", "_")}",
                    value == DBNull.Value ? (object)DBNull.Value : value);
            }

            if (!string.IsNullOrEmpty(pkColumn))
            {
                command.Parameters.AddWithValue("@pk_value", row[pkColumn, DataRowVersion.Original]);
            }

            return command;
        }

        private SQLiteCommand BuildExperimentInsertCommand(SQLiteConnection connection, DataRow row)
        {
            var command = connection.CreateCommand();
            var sbColumns = new StringBuilder();
            var sbValues = new StringBuilder();

            bool first = true;
            foreach (DataColumn col in myDataTable.Columns)
            {
                if (!first)
                {
                    sbColumns.Append(", ");
                    sbValues.Append(", ");
                }

                sbColumns.Append($"[{col.ColumnName}]");
                sbValues.Append($"@{col.ColumnName.Replace(".", "_")}");
                first = false;
            }

            command.CommandText = $"INSERT INTO [{currentExperimentTable}] ({sbColumns}) VALUES ({sbValues})";

            foreach (DataColumn col in myDataTable.Columns)
            {
                object value = row[col.ColumnName];
                command.Parameters.AddWithValue($"@{col.ColumnName.Replace(".", "_")}",
                    value == DBNull.Value ? (object)DBNull.Value : value);
            }

            return command;
        }

        private void btnAdd2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myDataTable == null)
                {
                    System.Windows.MessageBox.Show("请先加载数据!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataRow newRow = myDataTable.NewRow();

                if (myDataTable.PrimaryKey.Length > 0)
                {
                    string pkColumn = myDataTable.PrimaryKey[0].ColumnName;

                    int maxId = 0;
                    foreach (DataRow row in myDataTable.Rows)
                    {
                        if (row[pkColumn] != DBNull.Value)
                        {
                            int currentId;
                            if (int.TryParse(row[pkColumn].ToString(), out currentId))
                            {
                                if (currentId > maxId)
                                    maxId = currentId;
                            }
                        }
                    }

                    newRow[pkColumn] = maxId + 1;
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

            var result = System.Windows.MessageBox.Show(
                $"确定要删除选中的 {dataGridView2.SelectedItems.Count} 行吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var rowsToDelete = new List<DataRowView>();
                    foreach (var item in dataGridView2.SelectedItems)
                    {
                        if (item is DataRowView rowView)
                        {
                            rowsToDelete.Add(rowView);
                        }
                    }

                    foreach (var rowView in rowsToDelete)
                    {
                        rowView.Row.Delete();
                    }

                    btnSave2_Click(sender, e);

                    UpdateStatus($"已删除 {rowsToDelete.Count} 行");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnExport2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myDataTable == null || myDataTable.Rows.Count == 0)
                {
                    System.Windows.MessageBox.Show("没有数据可以导出!", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV 文件|*.csv|所有文件|*.*",
                    FileName = $"{currentExperimentTable}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();

                    var headers = myDataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                    sb.AppendLine(string.Join(",", headers));

                    foreach (DataRow row in myDataTable.Rows)
                    {
                        var values = row.ItemArray.Select(v =>
                        {
                            string value = v?.ToString() ?? "";
                            if (value.Contains(",") || value.Contains("\""))
                            {
                                value = "\"" + value.Replace("\"", "\"\"") + "\"";
                            }
                            return value;
                        });
                        sb.AppendLine(string.Join(",", values));
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                    System.Windows.MessageBox.Show(
                        $"✅ 导出成功!\n\n文件: {saveDialog.FileName}\n行数: {myDataTable.Rows.Count}",
                        "导出成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
    }