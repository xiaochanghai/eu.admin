-- 新增集团公司级 Token 额度策略与人工对账审计。
-- 所有者仅为 GroupId + CompanyId；UserId 不参与策略、周期或余额归属。
-- 先执行 075。若曾执行旧预览版（含 TenantId 或额度所有者 UserId），先执行 077 的空表保护转换。
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod', N'U') IS NULL OR OBJECT_ID(N'dbo.AgUserTokenQuotaReservation', N'U') IS NULL
 THROW 51000, 'Deploy quota ledger migration 075 first.', 1;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (
 OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaReservation'),
 OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'TenantId')
 THROW 51995, 'Legacy TenantId quota schema detected; execute guarded migration 077 before 076.', 1;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (
 OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'UserId')
 OR EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND name=N'UserId')
 THROW 51995, 'Legacy per-user quota schema detected; execute guarded migration 077 before 076.', 1;

IF OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy', N'U') IS NULL
CREATE TABLE dbo.AgUserTokenQuotaPolicy (
    ID uniqueidentifier NOT NULL,
    IsDeleted bit NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_IsDeleted DEFAULT (0),
    IsActive bit NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_IsActive DEFAULT (1),
    ImportDataId uniqueidentifier NULL,
    ModificationNum int NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_ModificationNum DEFAULT (0),
    Tag int NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_Tag DEFAULT (1),
    GroupId uniqueidentifier NULL,
    CompanyId uniqueidentifier NULL,
    AuditStatus varchar(32) NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_AuditStatus DEFAULT ('Add'),
    CurrentNode nvarchar(32) NULL,
    CreatedBy uniqueidentifier NULL,
    CreatedTime datetime NULL CONSTRAINT DF_AgUserTokenQuotaPolicy_CreatedTime DEFAULT (GETUTCDATE()),
    UpdateBy uniqueidentifier NULL,
    UpdateTime datetime NULL,
    UseDefaults bit NOT NULL,
    DailyTotalTokens bigint NULL,
    MonthlyTotalTokens bigint NULL,
    RequestReservationTokens bigint NULL,
    Revision bigint NOT NULL,
    CONSTRAINT PK_AgUserTokenQuotaPolicy PRIMARY KEY (ID)
);

IF OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment', N'U') IS NULL
CREATE TABLE dbo.AgUserTokenQuotaAdjustment (
    ID uniqueidentifier NOT NULL,
    IsDeleted bit NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_IsDeleted DEFAULT (0),
    IsActive bit NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_IsActive DEFAULT (1),
    ImportDataId uniqueidentifier NULL,
    ModificationNum int NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_ModificationNum DEFAULT (0),
    Tag int NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_Tag DEFAULT (1),
    GroupId uniqueidentifier NULL,
    CompanyId uniqueidentifier NULL,
    AuditStatus varchar(32) NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_AuditStatus DEFAULT ('Add'),
    CurrentNode nvarchar(32) NULL,
    CreatedBy uniqueidentifier NULL,
    CreatedTime datetime NULL CONSTRAINT DF_AgUserTokenQuotaAdjustment_CreatedTime DEFAULT (GETUTCDATE()),
    UpdateBy uniqueidentifier NULL,
    UpdateTime datetime NULL,
    OperatorId uniqueidentifier NOT NULL,
    Kind nvarchar(16) NOT NULL,
    TargetId uniqueidentifier NOT NULL,
    CommandHash nvarchar(64) NOT NULL,
    BeforeJson nvarchar(2000) NOT NULL,
    ResultJson nvarchar(2000) NOT NULL,
    Reason nvarchar(500) NOT NULL,
    EvidenceReference nvarchar(256) NOT NULL,
    AppliedAtUtc datetime2(7) NOT NULL,
    CONSTRAINT PK_AgUserTokenQuotaAdjustment PRIMARY KEY (ID)
);

DECLARE @Expected TABLE (TableName sysname, ColumnName sysname, TypeName sysname, MaxLength smallint, Nullable bit);
INSERT INTO @Expected VALUES
 (N'AgUserTokenQuotaPolicy', N'ID', N'uniqueidentifier', 16, 0),
 (N'AgUserTokenQuotaPolicy', N'GroupId', N'uniqueidentifier', 16, 1),
 (N'AgUserTokenQuotaPolicy', N'CompanyId', N'uniqueidentifier', 16, 1),
 (N'AgUserTokenQuotaPolicy', N'UseDefaults', N'bit', 1, 0),
 (N'AgUserTokenQuotaPolicy', N'DailyTotalTokens', N'bigint', 8, 1),
 (N'AgUserTokenQuotaPolicy', N'MonthlyTotalTokens', N'bigint', 8, 1),
 (N'AgUserTokenQuotaPolicy', N'RequestReservationTokens', N'bigint', 8, 1),
 (N'AgUserTokenQuotaPolicy', N'Revision', N'bigint', 8, 0),
 (N'AgUserTokenQuotaAdjustment', N'ID', N'uniqueidentifier', 16, 0),
 (N'AgUserTokenQuotaAdjustment', N'GroupId', N'uniqueidentifier', 16, 1),
 (N'AgUserTokenQuotaAdjustment', N'CompanyId', N'uniqueidentifier', 16, 1),
 (N'AgUserTokenQuotaAdjustment', N'OperatorId', N'uniqueidentifier', 16, 0),
 (N'AgUserTokenQuotaAdjustment', N'Kind', N'nvarchar', 32, 0),
 (N'AgUserTokenQuotaAdjustment', N'TargetId', N'uniqueidentifier', 16, 0),
 (N'AgUserTokenQuotaAdjustment', N'CommandHash', N'nvarchar', 128, 0),
 (N'AgUserTokenQuotaAdjustment', N'BeforeJson', N'nvarchar', 4000, 0),
 (N'AgUserTokenQuotaAdjustment', N'ResultJson', N'nvarchar', 4000, 0),
 (N'AgUserTokenQuotaAdjustment', N'Reason', N'nvarchar', 1000, 0),
 (N'AgUserTokenQuotaAdjustment', N'EvidenceReference', N'nvarchar', 512, 0),
 (N'AgUserTokenQuotaAdjustment', N'AppliedAtUtc', N'datetime2', 8, 0);
IF EXISTS (SELECT 1 FROM @Expected e
 LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.' + e.TableName) AND c.name=e.ColumnName
 LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id
 WHERE c.column_id IS NULL OR t.name<>e.TypeName OR c.max_length<>e.MaxLength OR c.is_nullable<>e.Nullable)
 THROW 51001, 'Incompatible quota management column; no conversion performed.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy') AND name=N'index_AgUserTokenQuotaPolicy_IsDeleted')
 CREATE INDEX index_AgUserTokenQuotaPolicy_IsDeleted ON dbo.AgUserTokenQuotaPolicy (IsDeleted);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy') AND name=N'index_AgUserTokenQuotaPolicy_Enabled')
 CREATE INDEX index_AgUserTokenQuotaPolicy_Enabled ON dbo.AgUserTokenQuotaPolicy (IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy') AND name=N'UX_AgUserTokenQuotaPolicy_Owner')
 CREATE UNIQUE INDEX UX_AgUserTokenQuotaPolicy_Owner ON dbo.AgUserTokenQuotaPolicy (GroupId, CompanyId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment') AND name=N'index_AgUserTokenQuotaAdjustment_IsDeleted')
 CREATE INDEX index_AgUserTokenQuotaAdjustment_IsDeleted ON dbo.AgUserTokenQuotaAdjustment (IsDeleted);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment') AND name=N'index_AgUserTokenQuotaAdjustment_Enabled')
 CREATE INDEX index_AgUserTokenQuotaAdjustment_Enabled ON dbo.AgUserTokenQuotaAdjustment (IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment') AND name=N'IX_AgUserTokenQuotaAdjustment_Owner')
 CREATE INDEX IX_AgUserTokenQuotaAdjustment_Owner ON dbo.AgUserTokenQuotaAdjustment (GroupId, CompanyId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy') AND i.name=N'UX_AgUserTokenQuotaPolicy_Owner'
 AND i.is_unique=1 AND i.is_disabled=0 AND i.has_filter=0
 AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=2
 AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'GroupId')
 AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=2 AND c.name=N'CompanyId'))
 THROW 51002, 'Incompatible quota owner unique index.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment') AND i.name=N'IX_AgUserTokenQuotaAdjustment_Owner'
 AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
 AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=2
 AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'GroupId')
 AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=2 AND c.name=N'CompanyId'))
 THROW 51002, 'Incompatible quota adjustment owner index.', 1;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
