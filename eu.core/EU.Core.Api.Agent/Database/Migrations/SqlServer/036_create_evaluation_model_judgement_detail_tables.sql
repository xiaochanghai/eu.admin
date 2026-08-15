-- Create normalized detail tables for Evaluation Model Judgements.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationModelJudgement', N'U') IS NULL
    THROW 51710, N'dbo.AgEvaluationModelJudgement does not exist. Run 035 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementEvaluator', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementEvaluator (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, JudgementId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL, Name VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgEvaluationModelJudgementEvaluator_Judgement FOREIGN KEY (JudgementId) REFERENCES dbo.AgEvaluationModelJudgement(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgEvaluationModelJudgementEvaluator_Order UNIQUE (JudgementId, Ordinal));

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementMinimumScore', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementMinimumScore (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, JudgementId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL, Name VARCHAR(128) NOT NULL, Score DECIMAL(9,4) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgEvaluationModelJudgementMinimumScore_Judgement FOREIGN KEY (JudgementId) REFERENCES dbo.AgEvaluationModelJudgement(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgEvaluationModelJudgementMinimumScore_Order UNIQUE (JudgementId, Ordinal),
            CONSTRAINT UX_AgEvaluationModelJudgementMinimumScore_Name UNIQUE (JudgementId, Name));

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementCase', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementCase (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, JudgementId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL, CaseId UNIQUEIDENTIFIER NOT NULL, CaseName VARCHAR(256) NOT NULL,
            UnifiedRunId UNIQUEIDENTIFIER NOT NULL, InputSha256 VARCHAR(64) NOT NULL, OutputSha256 VARCHAR(64) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgEvaluationModelJudgementCase_Judgement FOREIGN KEY (JudgementId) REFERENCES dbo.AgEvaluationModelJudgement(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgEvaluationModelJudgementCase_Order UNIQUE (JudgementId, Ordinal));

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementMetric', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementMetric (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, JudgementId UNIQUEIDENTIFIER NOT NULL,
            JudgementCaseId UNIQUEIDENTIFIER NOT NULL, Ordinal INT NOT NULL, Name VARCHAR(128) NOT NULL,
            Score DECIMAL(9,4) NULL, MinimumScore DECIMAL(9,4) NOT NULL, Passed BIT NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgEvaluationModelJudgementMetric_Case FOREIGN KEY (JudgementCaseId) REFERENCES dbo.AgEvaluationModelJudgementCase(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgEvaluationModelJudgementMetric_Order UNIQUE (JudgementCaseId, Ordinal));

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementDiagnostic', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementDiagnostic (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, JudgementId UNIQUEIDENTIFIER NOT NULL,
            JudgementMetricId UNIQUEIDENTIFIER NOT NULL, Ordinal INT NOT NULL, Code VARCHAR(256) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgEvaluationModelJudgementDiagnostic_Metric FOREIGN KEY (JudgementMetricId) REFERENCES dbo.AgEvaluationModelJudgementMetric(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgEvaluationModelJudgementDiagnostic_Order UNIQUE (JudgementMetricId, Ordinal));

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgementEvaluator') AND name = N'IX_AgEvaluationModelJudgementEvaluator_Judgement')
        CREATE INDEX IX_AgEvaluationModelJudgementEvaluator_Judgement ON dbo.AgEvaluationModelJudgementEvaluator(JudgementId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgementMinimumScore') AND name = N'IX_AgEvaluationModelJudgementMinimumScore_Judgement')
        CREATE INDEX IX_AgEvaluationModelJudgementMinimumScore_Judgement ON dbo.AgEvaluationModelJudgementMinimumScore(JudgementId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgementCase') AND name = N'IX_AgEvaluationModelJudgementCase_Judgement')
        CREATE INDEX IX_AgEvaluationModelJudgementCase_Judgement ON dbo.AgEvaluationModelJudgementCase(JudgementId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgementMetric') AND name = N'IX_AgEvaluationModelJudgementMetric_Judgement')
        CREATE INDEX IX_AgEvaluationModelJudgementMetric_Judgement ON dbo.AgEvaluationModelJudgementMetric(JudgementId, JudgementCaseId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgementDiagnostic') AND name = N'IX_AgEvaluationModelJudgementDiagnostic_Judgement')
        CREATE INDEX IX_AgEvaluationModelJudgementDiagnostic_Judgement ON dbo.AgEvaluationModelJudgementDiagnostic(JudgementId, JudgementMetricId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
