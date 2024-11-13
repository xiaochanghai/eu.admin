using EU.Core.Common;
using EU.Core.Model.Logs;
using EU.Core.Serilog.Extensions;
using Mapster;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using SqlSugar;

namespace EU.Core.Serilog.Sink;

public class LogBatchingSink : IBatchedLogEventSink
{
    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var sugar = App.GetService<ISqlSugarClient>(false);

        //await WriteSqlLog(sugar, batch.FilterSqlLog());
        //await WriteLogs(sugar, batch.FilterRemoveOtherLog());
    }

    public Task OnEmptyBatchAsync()
    {
        return Task.CompletedTask;
    }

    #region Write Log

    private async Task WriteLogs(ISqlSugarClient db, IEnumerable<LogEvent> batch)
    {
        if (!batch.Any())
            return;

        var group = batch.GroupBy(s => s.Level);
        foreach (var v in group)
        {
            switch (v.Key)
            {
                case LogEventLevel.Information:
                    await WriteInformationLog(db, v);
                    break;
                case LogEventLevel.Warning:
                    await WriteWarningLog(db, v);
                    break;
                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    await WriteErrorLog(db, v);
                    break;
            }
        }
    }

    private async Task WriteInformationLog(ISqlSugarClient db, IEnumerable<LogEvent> batch)
    {
        if (!batch.Any())
            return;

        var logs = new List<SmInformationLog>();
        foreach (var logEvent in batch)
        {
            var log = logEvent.Adapt<SmInformationLog>();
            log.Message = logEvent.RenderMessage();
            log.Properties = logEvent.Properties.ToJson();
            log.DateTime = logEvent.Timestamp.DateTime;
            logs.Add(log);
        }

        await db.Insertable(logs).SplitTable().ExecuteReturnSnowflakeIdAsync();
    }

    private async Task WriteWarningLog(ISqlSugarClient db, IEnumerable<LogEvent> batch)
    {
        if (!batch.Any())
            return;

        var logs = new List<SmWarningLog>();
        foreach (var logEvent in batch)
        {
            var log = logEvent.Adapt<SmWarningLog>();
            log.Message = logEvent.RenderMessage();
            log.Properties = logEvent.Properties.ToJson();
            log.DateTime = logEvent.Timestamp.DateTime;
            logs.Add(log);
        }

        await db.Insertable(logs).SplitTable().ExecuteReturnSnowflakeIdAsync();
    }

    private async Task WriteErrorLog(ISqlSugarClient db, IEnumerable<LogEvent> batch)
    {
        if (!batch.Any())
            return;

        var logs = new List<SmErrorLog>();
        foreach (var logEvent in batch)
        {
            var log = logEvent.Adapt<SmErrorLog>();
            log.Message = logEvent.RenderMessage();
            log.Properties = logEvent.Properties.ToJson();
            log.DateTime = logEvent.Timestamp.DateTime;
            logs.Add(log);
        }

        await db.Insertable(logs).SplitTable().ExecuteReturnSnowflakeIdAsync();
    }

    private async Task WriteSqlLog(ISqlSugarClient db, IEnumerable<LogEvent> batch)
    {
        if (!batch.Any())
            return;

        var logs = new List<SmSqlLog>();
        foreach (var logEvent in batch)
        {
            var log = logEvent.Adapt<SmSqlLog>();
            log.Message = logEvent.RenderMessage();
            log.Properties = logEvent.Properties.ToJson();
            log.DateTime = logEvent.Timestamp.DateTime;
            logs.Add(log);
        }

        await db.Insertable(logs).SplitTable().ExecuteReturnSnowflakeIdAsync();
    }

    #endregion
}