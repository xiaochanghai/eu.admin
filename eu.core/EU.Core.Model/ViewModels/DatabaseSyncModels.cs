namespace EU.Core.Model;

/// <summary>Agent 数据库同步请求。</summary>
public sealed class AgentDatabaseSyncRequest
{
    /// <summary>源 SqlSugar 连接配置标识。</summary>
    public string SourceConfigId { get; set; }

    /// <summary>目标 SqlSugar 连接配置标识。</summary>
    public string TargetConfigId { get; set; }

    /// <summary>指定需要同步的 Agent 表；为空时同步全部 Agent 表。</summary>
    public List<string> Tables { get; set; } = [];

    /// <summary>是否根据实体同步目标表结构。</summary>
    public bool SyncStructure { get; set; } = true;

    /// <summary>是否删除目标表原有数据并复制源表数据。</summary>
    public bool ReplaceData { get; set; }

    /// <summary>替换数据时必须显式确认。</summary>
    public bool ConfirmReplaceData { get; set; }

    /// <summary>单批写入行数。</summary>
    public int BatchSize { get; set; } = 1000;
}

/// <summary>Agent 数据库同步结果。</summary>
public sealed record AgentDatabaseSyncResult(
    string SourceConfigId,
    string TargetConfigId,
    bool StructureSynchronized,
    bool DataReplaced,
    long TotalRows,
    IReadOnlyList<AgentDatabaseSyncTableResult> Tables);

/// <summary>单张 Agent 表同步结果。</summary>
public sealed record AgentDatabaseSyncTableResult(
    string TableName,
    long SourceRows,
    long TargetRows);
