-- Create durable Agent task tables and maintain Chinese table/column descriptions.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AgAgentTask', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentTask (
        ID uniqueidentifier NOT NULL CONSTRAINT PK_AgAgentTask PRIMARY KEY,
        TenantId varchar(128) NOT NULL,
        UserId varchar(256) NOT NULL,
        Title nvarchar(256) NOT NULL,
        Description nvarchar(max) NULL,
        Input nvarchar(max) NOT NULL,
        InputSha256 varchar(64) NOT NULL,
        SourceType varchar(64) NULL,
        SourceId varchar(256) NULL,
        IdempotencyKey varchar(128) NULL,
        ConversationId uniqueidentifier NULL,
        CurrentRunId uniqueidentifier NULL,
        Status int NOT NULL CONSTRAINT DF_AgAgentTask_Status DEFAULT 0,
        Priority int NOT NULL CONSTRAINT DF_AgAgentTask_Priority DEFAULT 0,
        AttemptCount int NOT NULL CONSTRAINT DF_AgAgentTask_AttemptCount DEFAULT 0,
        MaximumAttempts int NOT NULL CONSTRAINT DF_AgAgentTask_MaximumAttempts DEFAULT 3,
        LogicalRevision bigint NOT NULL CONSTRAINT DF_AgAgentTask_LogicalRevision DEFAULT 0,
        AvailableAtUtc datetime2(7) NOT NULL,
        StartedAtUtc datetime2(7) NULL,
        FinishedAtUtc datetime2(7) NULL,
        LeaseOwner varchar(128) NULL,
        LeaseExpiresAtUtc datetime2(7) NULL,
        CheckpointKind varchar(64) NULL,
        CheckpointJson nvarchar(max) NULL,
        LastErrorCode varchar(128) NULL,
        LastErrorMessage nvarchar(max) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_AgAgentTask_IsDeleted DEFAULT 0,
        IsActive bit NULL CONSTRAINT DF_AgAgentTask_IsActive DEFAULT 1,
        ImportDataId uniqueidentifier NULL,
        ModificationNum int NULL CONSTRAINT DF_AgAgentTask_ModificationNum DEFAULT 0,
        Tag int NULL CONSTRAINT DF_AgAgentTask_Tag DEFAULT 1,
        GroupId uniqueidentifier NULL,
        CompanyId uniqueidentifier NULL,
        AuditStatus varchar(32) NULL CONSTRAINT DF_AgAgentTask_AuditStatus DEFAULT 'Add',
        CurrentNode nvarchar(32) NULL,
        CreatedBy uniqueidentifier NULL,
        CreatedTime datetime NULL CONSTRAINT DF_AgAgentTask_CreatedTime DEFAULT GETUTCDATE(),
        UpdateBy uniqueidentifier NULL,
        UpdateTime datetime NULL
    );
END;

IF OBJECT_ID(N'dbo.AgAgentTaskAttempt', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentTaskAttempt (
        ID uniqueidentifier NOT NULL CONSTRAINT PK_AgAgentTaskAttempt PRIMARY KEY,
        TaskId uniqueidentifier NOT NULL,
        AttemptNumber int NOT NULL,
        RunId uniqueidentifier NULL,
        Status int NOT NULL,
        WorkerId varchar(128) NOT NULL,
        StartedAtUtc datetime2(7) NOT NULL,
        FinishedAtUtc datetime2(7) NULL,
        ErrorCode varchar(128) NULL,
        ErrorMessage nvarchar(max) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_AgAgentTaskAttempt_IsDeleted DEFAULT 0,
        IsActive bit NULL CONSTRAINT DF_AgAgentTaskAttempt_IsActive DEFAULT 1,
        ImportDataId uniqueidentifier NULL,
        ModificationNum int NULL CONSTRAINT DF_AgAgentTaskAttempt_ModificationNum DEFAULT 0,
        Tag int NULL CONSTRAINT DF_AgAgentTaskAttempt_Tag DEFAULT 1,
        GroupId uniqueidentifier NULL,
        CompanyId uniqueidentifier NULL,
        AuditStatus varchar(32) NULL CONSTRAINT DF_AgAgentTaskAttempt_AuditStatus DEFAULT 'Add',
        CurrentNode nvarchar(32) NULL,
        CreatedBy uniqueidentifier NULL,
        CreatedTime datetime NULL CONSTRAINT DF_AgAgentTaskAttempt_CreatedTime DEFAULT GETUTCDATE(),
        UpdateBy uniqueidentifier NULL,
        UpdateTime datetime NULL,
        CONSTRAINT FK_AgAgentTaskAttempt_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.AgAgentTask(ID)
    );
END;

IF OBJECT_ID(N'dbo.AgAgentTaskEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentTaskEvent (
        ID uniqueidentifier NOT NULL CONSTRAINT PK_AgAgentTaskEvent PRIMARY KEY,
        TaskId uniqueidentifier NOT NULL,
        AttemptNumber int NULL,
        RunId uniqueidentifier NULL,
        Kind varchar(64) NOT NULL,
        Status int NOT NULL,
        WorkerId varchar(128) NULL,
        OccurredAtUtc datetime2(7) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_AgAgentTaskEvent_IsDeleted DEFAULT 0,
        IsActive bit NULL CONSTRAINT DF_AgAgentTaskEvent_IsActive DEFAULT 1,
        ImportDataId uniqueidentifier NULL,
        ModificationNum int NULL CONSTRAINT DF_AgAgentTaskEvent_ModificationNum DEFAULT 0,
        Tag int NULL CONSTRAINT DF_AgAgentTaskEvent_Tag DEFAULT 1,
        GroupId uniqueidentifier NULL,
        CompanyId uniqueidentifier NULL,
        AuditStatus varchar(32) NULL CONSTRAINT DF_AgAgentTaskEvent_AuditStatus DEFAULT 'Add',
        CurrentNode nvarchar(32) NULL,
        CreatedBy uniqueidentifier NULL,
        CreatedTime datetime NULL CONSTRAINT DF_AgAgentTaskEvent_CreatedTime DEFAULT GETUTCDATE(),
        UpdateBy uniqueidentifier NULL,
        UpdateTime datetime NULL,
        CONSTRAINT FK_AgAgentTaskEvent_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.AgAgentTask(ID)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentTask') AND name = N'ix_ag_agent_task_claim')
    CREATE INDEX ix_ag_agent_task_claim ON dbo.AgAgentTask(TenantId, Status, AvailableAtUtc, Priority) INCLUDE (LeaseExpiresAtUtc, AttemptCount, MaximumAttempts, LogicalRevision) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentTask') AND name = N'ix_ag_agent_task_user')
    CREATE INDEX ix_ag_agent_task_user ON dbo.AgAgentTask(TenantId, UserId, CreatedTime DESC) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentTask') AND name = N'ux_ag_agent_task_idempotency')
    CREATE UNIQUE INDEX ux_ag_agent_task_idempotency ON dbo.AgAgentTask(TenantId, UserId, IdempotencyKey) WHERE IsDeleted = 0 AND IdempotencyKey IS NOT NULL AND IdempotencyKey <> '';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentTaskAttempt') AND name = N'ux_ag_agent_task_attempt')
    CREATE UNIQUE INDEX ux_ag_agent_task_attempt ON dbo.AgAgentTaskAttempt(TaskId, AttemptNumber) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentTaskEvent') AND name = N'ix_ag_agent_task_event_time')
    CREATE INDEX ix_ag_agent_task_event_time ON dbo.AgAgentTaskEvent(TaskId, OccurredAtUtc DESC, CreatedTime DESC) WHERE IsDeleted = 0;

DECLARE @Descriptions TABLE (
    TableName sysname NOT NULL,
    ColumnName sysname NULL,
    Description nvarchar(4000) NOT NULL
);

INSERT @Descriptions (TableName, ColumnName, Description) VALUES
    (N'AgAgentTask', NULL, N'Agent 持久化后台任务'),
    (N'AgAgentTask', N'ID', N'任务主键'),
    (N'AgAgentTask', N'TenantId', N'租户标识'),
    (N'AgAgentTask', N'UserId', N'任务所属用户标识'),
    (N'AgAgentTask', N'Title', N'任务标题'),
    (N'AgAgentTask', N'Description', N'任务说明'),
    (N'AgAgentTask', N'Input', N'延迟执行时使用的任务输入'),
    (N'AgAgentTask', N'InputSha256', N'任务输入 SHA-256 摘要'),
    (N'AgAgentTask', N'SourceType', N'执行器来源类型，例如 chat'),
    (N'AgAgentTask', N'SourceId', N'任务来源业务标识'),
    (N'AgAgentTask', N'IdempotencyKey', N'同一租户和用户范围内的创建幂等键'),
    (N'AgAgentTask', N'ConversationId', N'关联对话标识'),
    (N'AgAgentTask', N'CurrentRunId', N'当前统一入口运行标识'),
    (N'AgAgentTask', N'Status', N'任务状态：0待执行、1执行中、2等待审批、3等待用户、4完成、5失败、6取消'),
    (N'AgAgentTask', N'Priority', N'领取优先级，数值越大越优先'),
    (N'AgAgentTask', N'AttemptCount', N'已创建的执行尝试次数'),
    (N'AgAgentTask', N'MaximumAttempts', N'失败重试允许的最大尝试次数'),
    (N'AgAgentTask', N'LogicalRevision', N'任务状态乐观并发修订号'),
    (N'AgAgentTask', N'AvailableAtUtc', N'允许被 Worker 领取的 UTC 时间'),
    (N'AgAgentTask', N'StartedAtUtc', N'任务首次开始执行的 UTC 时间'),
    (N'AgAgentTask', N'FinishedAtUtc', N'任务进入终态的 UTC 时间'),
    (N'AgAgentTask', N'LeaseOwner', N'当前执行租约持有者'),
    (N'AgAgentTask', N'LeaseExpiresAtUtc', N'当前执行租约到期 UTC 时间'),
    (N'AgAgentTask', N'CheckpointKind', N'持久化恢复检查点类型'),
    (N'AgAgentTask', N'CheckpointJson', N'仅包含恢复指针的检查点 JSON'),
    (N'AgAgentTask', N'LastErrorCode', N'最近一次执行错误码'),
    (N'AgAgentTask', N'LastErrorMessage', N'最近一次执行的受保护错误信息'),

    (N'AgAgentTaskAttempt', NULL, N'Agent 持久化任务执行尝试'),
    (N'AgAgentTaskAttempt', N'ID', N'执行尝试主键'),
    (N'AgAgentTaskAttempt', N'TaskId', N'关联任务标识'),
    (N'AgAgentTaskAttempt', N'AttemptNumber', N'任务内递增的尝试序号'),
    (N'AgAgentTaskAttempt', N'RunId', N'本次尝试关联的统一入口运行标识'),
    (N'AgAgentTaskAttempt', N'Status', N'尝试状态：0执行中、1完成、2失败、3取消、4暂停'),
    (N'AgAgentTaskAttempt', N'WorkerId', N'执行本次尝试的 Worker 标识'),
    (N'AgAgentTaskAttempt', N'StartedAtUtc', N'尝试开始 UTC 时间'),
    (N'AgAgentTaskAttempt', N'FinishedAtUtc', N'尝试结束 UTC 时间'),
    (N'AgAgentTaskAttempt', N'ErrorCode', N'尝试错误码'),
    (N'AgAgentTaskAttempt', N'ErrorMessage', N'尝试错误信息'),

    (N'AgAgentTaskEvent', NULL, N'Agent 持久化任务追加式生命周期事件'),
    (N'AgAgentTaskEvent', N'ID', N'任务事件主键'),
    (N'AgAgentTaskEvent', N'TaskId', N'关联任务标识'),
    (N'AgAgentTaskEvent', N'AttemptNumber', N'关联的执行尝试序号'),
    (N'AgAgentTaskEvent', N'RunId', N'关联的统一入口运行标识'),
    (N'AgAgentTaskEvent', N'Kind', N'事件类型'),
    (N'AgAgentTaskEvent', N'Status', N'事件发生后的任务状态'),
    (N'AgAgentTaskEvent', N'WorkerId', N'产生事件的 Worker 标识'),
    (N'AgAgentTaskEvent', N'OccurredAtUtc', N'事件发生 UTC 时间'),
    (N'AgAgentTaskEvent', N'PayloadJson', N'不包含原始输入的事件元数据 JSON');

DECLARE @TaskTables TABLE (TableName sysname NOT NULL);
INSERT @TaskTables (TableName) VALUES
    (N'AgAgentTask'),
    (N'AgAgentTaskAttempt'),
    (N'AgAgentTaskEvent');

INSERT @Descriptions (TableName, ColumnName, Description)
SELECT tables.TableName, common.ColumnName, common.Description
FROM @TaskTables tables
CROSS JOIN (VALUES
    (N'IsDeleted', N'逻辑删除标记'),
    (N'IsActive', N'有效状态标记'),
    (N'ImportDataId', N'导入模板标识'),
    (N'ModificationNum', N'修改次数'),
    (N'Tag', N'通用修改标记'),
    (N'GroupId', N'集团标识'),
    (N'CompanyId', N'公司标识'),
    (N'AuditStatus', N'审核状态'),
    (N'CurrentNode', N'当前流程节点'),
    (N'CreatedBy', N'创建人标识'),
    (N'CreatedTime', N'创建时间'),
    (N'UpdateBy', N'最后修改人标识'),
    (N'UpdateTime', N'最后修改时间')
) common(ColumnName, Description);

DECLARE
    @TableName sysname,
    @ColumnName sysname,
    @Description nvarchar(4000),
    @DescriptionExists bit;

DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR
    SELECT TableName, ColumnName, Description FROM @Descriptions;

OPEN descriptions;
FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @TableName, N'U') IS NULL
        THROW 52700, N'Agent task table is missing.', 1;
    IF @ColumnName IS NOT NULL AND COL_LENGTH(N'dbo.' + @TableName, @ColumnName) IS NULL
        THROW 52701, N'Agent task column is missing.', 1;

    SELECT @DescriptionExists = CASE WHEN EXISTS (
        SELECT 1
        FROM sys.extended_properties
        WHERE major_id = OBJECT_ID(N'dbo.' + @TableName)
          AND minor_id = CASE WHEN @ColumnName IS NULL
              THEN 0
              ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.' + @TableName), @ColumnName, 'ColumnId')
          END
          AND name = N'MS_Description'
    ) THEN 1 ELSE 0 END;

    IF @ColumnName IS NULL
    BEGIN
        IF @DescriptionExists = 1
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
    END
    ELSE
    BEGIN
        IF @DescriptionExists = 1
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
    END;

    FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;

COMMIT TRANSACTION;
