using EU.Core.Common.Helper;
using EU.Core.IServices;
using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到(任务类要去JobSetup添加注入)
/// </summary>
namespace EU.Core.Tasks
{
    public class DbBackup : JobBase, IJob
    {
        private readonly ILogger<DbBackup> _logger;

        public DbBackup(ILogger<DbBackup> logger, ISmQuartzJobServices tasksQzServices, ISmQuartzJobLogServices tasksQzLogServices) : base(tasksQzServices, tasksQzLogServices)
        {
            _logger = logger;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            // 可以直接获取 JobDetail 的值
            var jobKey = context.JobDetail.Key;
            var jobId = jobKey.Name;
            //Logger.WriteLog($"开始执行数据库备份！");
            await BackupDatabase();
            //Logger.WriteLog($"结束执行数据库备份！");
        }

        #region 数据库备份
        private static string DbBackupPath
        {
            get
            {
                try
                {
                    //string path = Path.Combine(Environment.CurrentDirectory, "data", "log.txt");
                    //string backupPath = Utility.GetAppSettingKey("DbBackupPath");
                    string backupPath = null;
                    if (string.IsNullOrEmpty(backupPath))
                    {
                        backupPath = @"C:\Backup\";
                    }
                    return backupPath;
                }
                catch (Exception)
                {
                    return @"C:\Backup\";
                }
            }
        }
        private async Task BackupDatabase()
        {
            try
            {
                string databaseName = DBHelper.DatabaseName;
                FileHelper.CreateDirectory(DbBackupPath + databaseName + "/");
                string saveAway = DbBackupPath + databaseName + "\\" + databaseName + "_" + DateTimeHelper.GetSysDateTimeString().Replace("/", "").Replace(" ", "").Replace(":", "") + ".bak";
                string cmdText = @"BACKUP DATABASE " + databaseName + " TO DISK='" + saveAway + "'";
                await DBHelper.ExecuteDMLAsync(cmdText);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }



}
