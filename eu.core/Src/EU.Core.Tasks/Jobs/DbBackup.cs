using EU.Core.Common;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EU.Core.Tasks;

public class DbBackup : JobBase, IJob
{
    private readonly ILogger<DbBackup> _logger;

    public DbBackup(ILogger<DbBackup> logger, ISmQuartzJobServices tasksQzServices, ISmQuartzJobLogServices tasksQzLogServices) : base(tasksQzServices, tasksQzLogServices)
    {
        _logger = logger;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        if (DBHelper.MySql)
        {
            await BackupMySqlDatabase();
        }
        else
        {
            await BackupDatabase();
        }
    }

    #region 数据库备份
    private static string DefaultBackupPath => ComputerHelper.IsUnix() ? "/var/backups/" : @"C:\Backup\";
    private async Task BackupDatabase()
    {
        try
        {
            string databaseName = DBHelper.DatabaseName;
            string backupDir = Path.Combine(DefaultBackupPath, databaseName);
            FileHelper.CreateDirectory(backupDir);
            string saveAway = Path.Combine(backupDir, databaseName + "_" + DateTimeHelper.GetSysDateTimeString().Replace("/", "").Replace(" ", "").Replace(":", "") + ".bak");
            string cmdText = @"BACKUP DATABASE " + databaseName + " TO DISK='" + saveAway + "'";
            await DBHelper.ExecuteDMLAsync(cmdText);
            _logger.LogInformation("SQL Server 数据库备份完成: {SavePath}", saveAway);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server 数据库备份失败");
            throw;
        }
    }

    /// <summary>
    /// 纯 ADO.NET 方式备份 MySQL 数据库（不依赖 mysqldump）
    /// 导出表结构 + 表数据 + 视图 + 存储过程
    /// </summary>
    private async Task BackupMySqlDatabase()
    {
        try
        {
            var connStr = DBHelper.Instance.Connection.ConnectionString;
            var builder = new MySqlConnectionStringBuilder(connStr);
            string databaseName = builder.Database;
            string timestamp = DateTimeHelper.GetSysDateTimeString().Replace("/", "").Replace(" ", "").Replace(":", "");
            string backupDir = Path.Combine(DefaultBackupPath, databaseName);
            string savePath = Path.Combine(backupDir, databaseName + "_" + timestamp + ".sql");

            FileHelper.CreateDirectory(backupDir);

            using var writer = new StreamWriter(savePath, false, Encoding.UTF8);
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            // 设置编码
            await writer.WriteLineAsync("-- MySQL Database Backup");
            await writer.WriteLineAsync($"-- Database: {databaseName}");
            await writer.WriteLineAsync($"-- Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("SET NAMES utf8mb4;");
            await writer.WriteLineAsync("SET FOREIGN_KEY_CHECKS = 0;");
            await writer.WriteLineAsync("SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';");
            await writer.WriteLineAsync();

            // 1. 导出所有表结构 + 数据
            var tables = await GetTablesAsync(conn, databaseName);
            foreach (var table in tables)
            {
                await writer.WriteLineAsync($"-- ----------------------------");
                await writer.WriteLineAsync($"-- Table structure for {table}");
                await writer.WriteLineAsync($"-- ----------------------------");
                await writer.WriteLineAsync($"DROP TABLE IF EXISTS `{table}`;");
                await writer.WriteLineAsync();

                // 获取 CREATE TABLE 语句
                var createTableSql = await GetCreateTableSqlAsync(conn, table);
                await writer.WriteLineAsync(createTableSql);
                await writer.WriteLineAsync();

                // 导出数据
                await writer.WriteLineAsync("START TRANSACTION;");
                var rowCount = await DumpTableDataAsync(conn, writer, table);
                await writer.WriteLineAsync("COMMIT;");
                await writer.WriteLineAsync($"-- Records: {rowCount} rows");
                await writer.WriteLineAsync();
            }

            // 2. 导出视图
            var views = await GetViewsAsync(conn, databaseName);
            if (views.Count > 0)
            {
                foreach (var view in views)
                {
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"-- View structure for {view}");
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"DROP VIEW IF EXISTS `{view}`;");
                    await writer.WriteLineAsync();

                    var createViewSql = await GetCreateViewSqlAsync(conn, view);
                    await writer.WriteLineAsync(createViewSql);
                    await writer.WriteLineAsync();
                }
            }

            // 3. 导出触发器
            var triggers = await GetTriggersAsync(conn, databaseName);
            if (triggers.Count > 0)
            {
                foreach (var trigger in triggers)
                {
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"-- Trigger structure for {trigger.Name}");
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"DROP TRIGGER IF EXISTS `{trigger.Name}`;");
                    await writer.WriteLineAsync("DELIMITER ;;");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync(trigger.Body);
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync(";;");
                    await writer.WriteLineAsync("DELIMITER ;");
                    await writer.WriteLineAsync();
                }
            }

            // 4. 导出存储过程和函数
            var routines = await GetRoutinesAsync(conn, databaseName);
            if (routines.Count > 0)
            {
                foreach (var routine in routines)
                {
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"-- {routine.Type} structure for {routine.Name}");
                    await writer.WriteLineAsync($"-- ----------------------------");
                    await writer.WriteLineAsync($"DROP {routine.Type} IF EXISTS `{routine.Name}`;");
                    await writer.WriteLineAsync("DELIMITER ;;");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync(routine.Body);
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync(";;");
                    await writer.WriteLineAsync("DELIMITER ;");
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteLineAsync("SET FOREIGN_KEY_CHECKS = 1;");

            _logger.LogInformation("MySQL 数据库备份完成: {SavePath} ({TableCount} tables, {ViewCount} views, {TriggerCount} triggers, {RoutineCount} routines)",
                savePath, tables.Count, views.Count, triggers.Count, routines.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL 数据库备份失败");
            throw;
        }
    }

    private static async Task<List<string>> GetTablesAsync(MySqlConnection conn, string database)
    {
        var tables = new List<string>();
        using var cmd = new MySqlCommand(
            "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME", conn);
        cmd.Parameters.AddWithValue("@schema", database);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static async Task<string> GetCreateTableSqlAsync(MySqlConnection conn, string table)
    {
        using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return reader.GetString(1) + ";";
        return "";
    }

    private static async Task<int> DumpTableDataAsync(MySqlConnection conn, StreamWriter writer, string table)
    {
        var count = 0;
        using var cmd = new MySqlCommand($"SELECT * FROM `{table}`", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        // 获取列名和列数
        var fieldCount = reader.FieldCount;
        var columnNames = new string[fieldCount];
        for (int i = 0; i < fieldCount; i++)
            columnNames[i] = reader.GetName(i);

        // 批量 INSERT，每 500 行拼成一条多值 INSERT
        var valuesBatch = new List<string>();
        var batchSize = 500;
        var columnList = string.Join(",", columnNames.Select(c => $"`{c}`"));

        await writer.WriteLineAsync($"-- ----------------------------");
        await writer.WriteLineAsync($"-- Records of {table}");
        await writer.WriteLineAsync($"-- ----------------------------");

        while (await reader.ReadAsync())
        {
            var values = new object[fieldCount];
            reader.GetValues(values);

            valuesBatch.Add("(" + string.Join(",", values.Select(FormatValue)) + ")");

            count++;
            if (count % batchSize == 0)
            {
                await FlushInsertBatchAsync(writer, table, columnList, valuesBatch);
            }
        }

        if (valuesBatch.Count > 0)
            await FlushInsertBatchAsync(writer, table, columnList, valuesBatch);

        return count;
    }

    private static Task FlushInsertBatchAsync(StreamWriter writer, string table, string columnList, List<string> valuesBatch)
    {
        var sql = $"INSERT INTO `{table}` ({columnList}) VALUES{Environment.NewLine}{string.Join($",{Environment.NewLine}", valuesBatch)};";
        valuesBatch.Clear();
        return writer.WriteLineAsync(sql);
    }

    private static string FormatValue(object value)
    {
        if (value == null || value == DBNull.Value)
            return "NULL";

        switch (value)
        {
            case bool b: return b ? "1" : "0";
            case byte bb: return bb.ToString();
            case sbyte sb: return sb.ToString();
            case short s: return s.ToString();
            case ushort us: return us.ToString();
            case int i: return i.ToString();
            case uint ui: return ui.ToString();
            case long l: return l.ToString();
            case ulong ul: return ul.ToString();
            case float f: return f.ToString("G", CultureInfo.InvariantCulture);
            case double d: return d.ToString("G", CultureInfo.InvariantCulture);
            case decimal m: return m.ToString(CultureInfo.InvariantCulture);
            case DateTime dt: return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            case TimeSpan ts: return $"'{ts:c}'";
            case Guid g: return $"'{g:D}'";
            case byte[] bytes: return $"X'{BitConverter.ToString(bytes).Replace("-", "")}'";
            default:
                var str = value.ToString();
                var escaped = str.Replace("\\", "\\\\").Replace("'", "\\'");
                return $"'{escaped}'";
        }
    }

    private static async Task<List<string>> GetViewsAsync(MySqlConnection conn, string database)
    {
        var views = new List<string>();
        using var cmd = new MySqlCommand(
            "SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'VIEW' ORDER BY TABLE_NAME", conn);
        cmd.Parameters.AddWithValue("@schema", database);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            views.Add(reader.GetString(0));
        return views;
    }

    private static async Task<string> GetCreateViewSqlAsync(MySqlConnection conn, string view)
    {
        using var cmd = new MySqlCommand($"SHOW CREATE VIEW `{view}`", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return StripDefiner(reader.GetString(1)) + ";";
        }
        return "";
    }

    private static async Task<List<(string Name, string Type, string Body)>> GetRoutinesAsync(MySqlConnection conn, string database)
    {
        var routines = new List<(string Name, string Type, string Body)>();
        // 先查名称和类型
        using var nameCmd = new MySqlCommand(
            "SELECT ROUTINE_NAME, ROUTINE_TYPE FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = @schema ORDER BY ROUTINE_TYPE, ROUTINE_NAME", conn);
        nameCmd.Parameters.AddWithValue("@schema", database);
        using var nameReader = await nameCmd.ExecuteReaderAsync();
        var names = new List<(string Name, string Type)>();
        while (await nameReader.ReadAsync())
            names.Add((nameReader.GetString(0), nameReader.GetString(1)));
        nameReader.Close();

        // 用 SHOW CREATE 获取完整定义，避免 information_schema.ROUTINE_DEFINITION 截断
        foreach (var (name, type) in names)
        {
            string showCmdText = type == "PROCEDURE" ? $"SHOW CREATE PROCEDURE `{name}`" : $"SHOW CREATE FUNCTION `{name}`";
            using var cmd = new MySqlCommand(showCmdText, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var body = StripDefiner(reader.GetString(2));
                routines.Add((name, type, body));
            }
            reader.Close();
        }
        return routines;
    }

    private static async Task<List<(string Name, string Body)>> GetTriggersAsync(MySqlConnection conn, string database)
    {
        var triggers = new List<(string Name, string Body)>();
        using var nameCmd = new MySqlCommand(
            "SELECT TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = @schema ORDER BY TRIGGER_NAME", conn);
        nameCmd.Parameters.AddWithValue("@schema", database);
        using var nameReader = await nameCmd.ExecuteReaderAsync();
        var names = new List<string>();
        while (await nameReader.ReadAsync())
            names.Add(nameReader.GetString(0));
        nameReader.Close();

        foreach (var name in names)
        {
            using var cmd = new MySqlCommand($"SHOW CREATE TRIGGER `{name}`", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var sqlStatement = StripDefiner(reader.GetString(2)); // SQL Original Statement
                triggers.Add((name, sqlStatement));
            }
            reader.Close();
        }
        return triggers;
    }

    private static string StripDefiner(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        sql = Regex.Replace(sql, @"\s+DEFINER=`[^`]+`@`[^`]+`", "", RegexOptions.IgnoreCase);
        sql = Regex.Replace(sql, @"DEFINER=`[^`]+`@`[^`]+`\s+", "", RegexOptions.IgnoreCase);
        return sql.Trim();
    }

    #endregion
}
