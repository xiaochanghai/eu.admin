namespace EU.Core.Model.Logs;

[Tenant("log")]
[SplitTable(SplitType.Month)] //按月分表 （自带分表支持 年、季、月、周、日）
[SugarTable($@"{nameof(SmWarningLog)}_{{year}}{{month}}{{day}}")]
public class SmWarningLog: BaseLog
{
    
}