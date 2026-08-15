-- Create normalized Evaluation Suite version, case, and ordered rule tables.
-- Run after 025. All persisted character fields use VARCHAR.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationSuite', N'U') IS NULL
    THROW 51510, N'dbo.AgEvaluationSuite is missing.', 1;
GO

DECLARE @BaseColumns NVARCHAR(MAX) = N'
    IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1),
    ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL DEFAULT (0),
    Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
    AuditStatus VARCHAR(32) NULL DEFAULT (''Add''), CurrentNode VARCHAR(32) NULL,
    CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
    UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL';
DECLARE @Sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.AgEvaluationSuiteVersion', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationSuiteVersion (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, SuiteId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL, Label VARCHAR(128) NULL, IsDraft BIT NULL,
        ContentSha256 VARCHAR(64) NULL, PublishedAtUtc DATETIME2(7) NULL,
        PublishedByUserId VARCHAR(256) NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationSuiteVersion_Suite FOREIGN KEY (SuiteId) REFERENCES dbo.AgEvaluationSuite(ID));';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgEvaluationCase', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationCase (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, SuiteId UNIQUEIDENTIFIER NULL,
        VersionId UNIQUEIDENTIFIER NULL, Ordinal INT NULL, CaseId UNIQUEIDENTIFIER NULL,
        Name VARCHAR(256) NULL, Input VARCHAR(MAX) NULL,
        TargetAgentId UNIQUEIDENTIFIER NULL, TargetAgentVersionId UNIQUEIDENTIFIER NULL,
        ExpectedStatus VARCHAR(32) NULL, MaximumToolCalls INT NULL,
        MaximumDurationMilliseconds BIGINT NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationCase_Suite FOREIGN KEY (SuiteId) REFERENCES dbo.AgEvaluationSuite(ID),
        CONSTRAINT FK_AgEvaluationCase_Version FOREIGN KEY (VersionId) REFERENCES dbo.AgEvaluationSuiteVersion(ID));';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgEvaluationCaseRule', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgEvaluationCaseRule (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, SuiteId UNIQUEIDENTIFIER NULL,
        VersionId UNIQUEIDENTIFIER NULL, EvaluationCaseId UNIQUEIDENTIFIER NULL,
        RuleType VARCHAR(32) NULL, Ordinal INT NULL, Value VARCHAR(512) NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgEvaluationCaseRule_Suite FOREIGN KEY (SuiteId) REFERENCES dbo.AgEvaluationSuite(ID),
        CONSTRAINT FK_AgEvaluationCaseRule_Version FOREIGN KEY (VersionId) REFERENCES dbo.AgEvaluationSuiteVersion(ID),
        CONSTRAINT FK_AgEvaluationCaseRule_Case FOREIGN KEY (EvaluationCaseId) REFERENCES dbo.AgEvaluationCase(ID));';
    EXEC sys.sp_executesql @Sql;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuiteVersion') AND name = N'ux_ag_evaluation_suite_version_ordinal')
    CREATE UNIQUE INDEX ux_ag_evaluation_suite_version_ordinal ON dbo.AgEvaluationSuiteVersion(SuiteId, Ordinal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuiteVersion') AND name = N'ux_ag_evaluation_suite_version_draft')
    CREATE UNIQUE INDEX ux_ag_evaluation_suite_version_draft ON dbo.AgEvaluationSuiteVersion(SuiteId) WHERE IsDraft = 1 AND IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationCase') AND name = N'ux_ag_evaluation_case_contract_id')
    CREATE UNIQUE INDEX ux_ag_evaluation_case_contract_id ON dbo.AgEvaluationCase(VersionId, CaseId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationCaseRule') AND name = N'ux_ag_evaluation_case_rule_order')
    CREATE UNIQUE INDEX ux_ag_evaluation_case_rule_order ON dbo.AgEvaluationCaseRule(EvaluationCaseId, RuleType, Ordinal);

DECLARE @TableName SYSNAME, @IndexSql NVARCHAR(MAX);
DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE name IN (N'AgEvaluationSuiteVersion', N'AgEvaluationCase', N'AgEvaluationCaseRule');
OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ix_' + LOWER(@TableName) + N'_is_deleted')
    BEGIN
        SET @IndexSql = N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_deleted') + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsDeleted);';
        EXEC sys.sp_executesql @IndexSql;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ix_' + LOWER(@TableName) + N'_is_active')
    BEGIN
        SET @IndexSql = N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_active') + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsActive);';
        EXEC sys.sp_executesql @IndexSql;
    END;
    FETCH NEXT FROM table_cursor INTO @TableName;
END;
CLOSE table_cursor;
DEALLOCATE table_cursor;
GO

PRINT N'Normalized Evaluation Suite tables are ready; generate and execute evaluation_suite_normalized_data.generated.sql next.';
GO
