-- Create normalized Evaluation Batch case, check, and observation tables.
-- Run after 030. All persisted character fields use VARCHAR.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationBatch', N'U') IS NULL
    THROW 51610, N'dbo.AgEvaluationBatch is missing.', 1;
GO

DECLARE @BaseColumns NVARCHAR(MAX) = N'
    IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1),
    ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL DEFAULT (0),
    Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
    AuditStatus VARCHAR(32) NULL DEFAULT (''Add''), CurrentNode VARCHAR(32) NULL,
    CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
    UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL';
DECLARE @Sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.AgEvaluationBatchCase', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationBatchCase (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, BatchId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL, CaseId UNIQUEIDENTIFIER NULL, CaseName VARCHAR(256) NULL,
        TargetAgentId UNIQUEIDENTIFIER NULL, TargetAgentVersionId UNIQUEIDENTIFIER NULL,
        Status VARCHAR(32) NULL, UnifiedRunId UNIQUEIDENTIFIER NULL,
        UnifiedRunStatus VARCHAR(32) NULL, ErrorCode VARCHAR(128) NULL,
        DurationMilliseconds BIGINT NULL, ToolCallCount INT NULL,
        ReportEvaluatedAtUtc DATETIME2(7) NULL, ReportPassed BIT NULL,
        ReportScore DECIMAL(9,4) NULL, OutputSha256 VARCHAR(64) NULL,
        OutputUtf8Bytes INT NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationBatchCase_Batch FOREIGN KEY (BatchId) REFERENCES dbo.AgEvaluationBatch(ID));';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgEvaluationBatchCheck', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationBatchCheck (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, BatchId UNIQUEIDENTIFIER NULL,
        BatchCaseId UNIQUEIDENTIFIER NULL, Ordinal INT NULL, Code VARCHAR(64) NULL,
        Passed BIT NULL, Expected VARCHAR(1024) NULL, Actual VARCHAR(1024) NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationBatchCheck_Batch FOREIGN KEY (BatchId) REFERENCES dbo.AgEvaluationBatch(ID),
        CONSTRAINT FK_AgEvaluationBatchCheck_Case FOREIGN KEY (BatchCaseId) REFERENCES dbo.AgEvaluationBatchCase(ID));';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgEvaluationBatchObservation', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationBatchObservation (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, BatchId UNIQUEIDENTIFIER NULL,
        BatchCaseId UNIQUEIDENTIFIER NULL, ObservationType VARCHAR(32) NULL,
        Ordinal INT NULL, Value VARCHAR(256) NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationBatchObservation_Batch FOREIGN KEY (BatchId) REFERENCES dbo.AgEvaluationBatch(ID),
        CONSTRAINT FK_AgEvaluationBatchObservation_Case FOREIGN KEY (BatchCaseId) REFERENCES dbo.AgEvaluationBatchCase(ID));';
    EXEC sys.sp_executesql @Sql;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatchCase') AND name = N'ux_ag_evaluation_batch_case_order')
    CREATE UNIQUE INDEX ux_ag_evaluation_batch_case_order ON dbo.AgEvaluationBatchCase(BatchId, Ordinal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatchCheck') AND name = N'ux_ag_evaluation_batch_check_order')
    CREATE UNIQUE INDEX ux_ag_evaluation_batch_check_order ON dbo.AgEvaluationBatchCheck(BatchCaseId, Ordinal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatchObservation') AND name = N'ux_ag_evaluation_batch_observation_order')
    CREATE UNIQUE INDEX ux_ag_evaluation_batch_observation_order ON dbo.AgEvaluationBatchObservation(BatchCaseId, ObservationType, Ordinal);

DECLARE @TableName SYSNAME, @IndexSql NVARCHAR(MAX);
DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE name IN (N'AgEvaluationBatchCase', N'AgEvaluationBatchCheck', N'AgEvaluationBatchObservation');
OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ix_' + LOWER(@TableName) + N'_is_deleted')
    BEGIN
        SET @IndexSql = N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_deleted') + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsDeleted);';
        EXEC sys.sp_executesql @IndexSql;
    END;
    FETCH NEXT FROM table_cursor INTO @TableName;
END;
CLOSE table_cursor;
DEALLOCATE table_cursor;
GO

PRINT N'Normalized Evaluation Batch detail tables are ready; generate and execute evaluation_batch_normalized_data.generated.sql next.';
GO
