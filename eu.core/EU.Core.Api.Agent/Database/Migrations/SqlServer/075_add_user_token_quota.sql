-- 新增内部配额账本；所有者仅为 GroupId + CompanyId，同一范围内所有用户和 Agent 共用。
-- 手动停写、备份后执行；不清空账本，不自动迁移，不退还未知/在途预占。
-- 可重复执行；已存在结构不兼容则停止，不进行破坏性转换。
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaReservation'), OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'TenantId')
        THROW 51995, N'Legacy TenantId quota schema detected; use migration 077 for the guarded empty-table conversion before rerunning 075.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'UserId')
       OR EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND name=N'UserId')
        THROW 51995, N'Legacy per-user quota schema detected; use migration 077 for the guarded empty-table conversion before rerunning 075.', 1;
    IF OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod', N'U') IS NULL
    CREATE TABLE dbo.AgUserTokenQuotaPeriod (
        ID uniqueidentifier NOT NULL,
        IsDeleted bit NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_IsDeleted DEFAULT (0),
        IsActive bit NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_IsActive DEFAULT (1),
        ImportDataId uniqueidentifier NULL,
        ModificationNum int NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_ModificationNum DEFAULT (0),
        Tag int NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_Tag DEFAULT (1),
        GroupId uniqueidentifier NULL,
        CompanyId uniqueidentifier NULL,
        AuditStatus varchar(32) NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_AuditStatus DEFAULT ('Add'),
        CurrentNode nvarchar(32) NULL,
        CreatedBy uniqueidentifier NULL,
        CreatedTime datetime NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_CreatedTime DEFAULT (GETUTCDATE()),
        UpdateBy uniqueidentifier NULL,
        UpdateTime datetime NULL,
        PeriodKind int NOT NULL,
        PeriodKey nvarchar(160) NOT NULL,
        StartUtc datetime2(7) NOT NULL,
        EndUtc datetime2(7) NOT NULL,
        UsedTokens bigint NOT NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_UsedTokens DEFAULT (0),
        ReservedTokens bigint NOT NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_ReservedTokens DEFAULT (0),
        HasUnknownUsage bit NOT NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_HasUnknownUsage DEFAULT (0),
        Revision bigint NOT NULL CONSTRAINT DF_AgUserTokenQuotaPeriod_Revision DEFAULT (0),
        CONSTRAINT PK_AgUserTokenQuotaPeriod PRIMARY KEY (ID)
    );
    IF OBJECT_ID(N'dbo.AgUserTokenQuotaReservation', N'U') IS NULL
    CREATE TABLE dbo.AgUserTokenQuotaReservation (
        ID uniqueidentifier NOT NULL,
        IsDeleted bit NULL CONSTRAINT DF_AgUserTokenQuotaReservation_IsDeleted DEFAULT (0),
        IsActive bit NULL CONSTRAINT DF_AgUserTokenQuotaReservation_IsActive DEFAULT (1),
        ImportDataId uniqueidentifier NULL,
        ModificationNum int NULL CONSTRAINT DF_AgUserTokenQuotaReservation_ModificationNum DEFAULT (0),
        Tag int NULL CONSTRAINT DF_AgUserTokenQuotaReservation_Tag DEFAULT (1),
        GroupId uniqueidentifier NULL,
        CompanyId uniqueidentifier NULL,
        AuditStatus varchar(32) NULL CONSTRAINT DF_AgUserTokenQuotaReservation_AuditStatus DEFAULT ('Add'),
        CurrentNode nvarchar(32) NULL,
        CreatedBy uniqueidentifier NULL,
        CreatedTime datetime NULL CONSTRAINT DF_AgUserTokenQuotaReservation_CreatedTime DEFAULT (GETUTCDATE()),
        UpdateBy uniqueidentifier NULL,
        UpdateTime datetime NULL,
        ConsumerUserId uniqueidentifier NOT NULL,
        RunId uniqueidentifier NOT NULL,
        DailyPeriodId uniqueidentifier NULL,
        MonthlyPeriodId uniqueidentifier NULL,
        ReservedTokens bigint NOT NULL,
        ActualTokens bigint NULL,
        State int NOT NULL CONSTRAINT DF_AgUserTokenQuotaReservation_State DEFAULT (0),
        SettledAtUtc datetime2(7) NULL,
        CONSTRAINT PK_AgUserTokenQuotaReservation PRIMARY KEY (ID)
    );
    DECLARE @Expected TABLE (TableName sysname, ColumnName sysname, TypeName sysname, MaxLength smallint, Nullable bit, Description nvarchar(256));
    INSERT INTO @Expected VALUES
        ('AgUserTokenQuotaPeriod', 'ID', 'uniqueidentifier', 16, 0, N'主键'),
        ('AgUserTokenQuotaPeriod', 'IsDeleted', 'bit', 1, 1, N'逻辑删除标记'),
        ('AgUserTokenQuotaPeriod', 'IsActive', 'bit', 1, 1, N'有效标志'),
        ('AgUserTokenQuotaPeriod', 'ImportDataId', 'uniqueidentifier', 16, 1, N'导入模板标识'),
        ('AgUserTokenQuotaPeriod', 'ModificationNum', 'int', 4, 1, N'修改次数'),
        ('AgUserTokenQuotaPeriod', 'Tag', 'int', 4, 1, N'修改标志'),
        ('AgUserTokenQuotaPeriod', 'GroupId', 'uniqueidentifier', 16, 1, N'集团标识，仅插入'),
        ('AgUserTokenQuotaPeriod', 'CompanyId', 'uniqueidentifier', 16, 1, N'公司标识，仅插入'),
        ('AgUserTokenQuotaPeriod', 'AuditStatus', 'varchar', 32, 1, N'审核状态'),
        ('AgUserTokenQuotaPeriod', 'CurrentNode', 'nvarchar', 64, 1, N'当前流程节点'),
        ('AgUserTokenQuotaPeriod', 'CreatedBy', 'uniqueidentifier', 16, 1, N'创建人，仅插入'),
        ('AgUserTokenQuotaPeriod', 'CreatedTime', 'datetime', 8, 1, N'创建时间，仅插入，服务端时间'),
        ('AgUserTokenQuotaPeriod', 'UpdateBy', 'uniqueidentifier', 16, 1, N'更新人，仅更新'),
        ('AgUserTokenQuotaPeriod', 'UpdateTime', 'datetime', 8, 1, N'更新时间，仅更新'),
        ('AgUserTokenQuotaPeriod', 'PeriodKind', 'int', 4, 0, N'周期种类：0 日，1 月'),
        ('AgUserTokenQuotaPeriod', 'PeriodKey', 'nvarchar', 320, 0, N'本地日期及配置时区的周期键'),
        ('AgUserTokenQuotaPeriod', 'StartUtc', 'datetime2', 8, 0, N'周期含起点 UTC'),
        ('AgUserTokenQuotaPeriod', 'EndUtc', 'datetime2', 8, 0, N'周期不含终点 UTC'),
        ('AgUserTokenQuotaPeriod', 'UsedTokens', 'bigint', 8, 0, N'已结算实际 Token'),
        ('AgUserTokenQuotaPeriod', 'ReservedTokens', 'bigint', 8, 0, N'未结算预占，禁止自动过期退还'),
        ('AgUserTokenQuotaPeriod', 'HasUnknownUsage', 'bit', 1, 0, N'未知用量冻结标记'),
        ('AgUserTokenQuotaPeriod', 'Revision', 'bigint', 8, 0, N'乐观并发版本'),
        ('AgUserTokenQuotaReservation', 'ID', 'uniqueidentifier', 16, 0, N'主键'),
        ('AgUserTokenQuotaReservation', 'IsDeleted', 'bit', 1, 1, N'逻辑删除标记'),
        ('AgUserTokenQuotaReservation', 'IsActive', 'bit', 1, 1, N'有效标志'),
        ('AgUserTokenQuotaReservation', 'ImportDataId', 'uniqueidentifier', 16, 1, N'导入模板标识'),
        ('AgUserTokenQuotaReservation', 'ModificationNum', 'int', 4, 1, N'修改次数'),
        ('AgUserTokenQuotaReservation', 'Tag', 'int', 4, 1, N'修改标志'),
        ('AgUserTokenQuotaReservation', 'GroupId', 'uniqueidentifier', 16, 1, N'集团标识，仅插入'),
        ('AgUserTokenQuotaReservation', 'CompanyId', 'uniqueidentifier', 16, 1, N'公司标识，仅插入'),
        ('AgUserTokenQuotaReservation', 'AuditStatus', 'varchar', 32, 1, N'审核状态'),
        ('AgUserTokenQuotaReservation', 'CurrentNode', 'nvarchar', 64, 1, N'当前流程节点'),
        ('AgUserTokenQuotaReservation', 'CreatedBy', 'uniqueidentifier', 16, 1, N'创建人，仅插入'),
        ('AgUserTokenQuotaReservation', 'CreatedTime', 'datetime', 8, 1, N'创建时间，仅插入，服务端时间'),
        ('AgUserTokenQuotaReservation', 'UpdateBy', 'uniqueidentifier', 16, 1, N'更新人，仅更新'),
        ('AgUserTokenQuotaReservation', 'UpdateTime', 'datetime', 8, 1, N'更新时间，仅更新'),
        ('AgUserTokenQuotaReservation', 'ConsumerUserId', 'uniqueidentifier', 16, 0, N'实际调用用户，仅追踪，不参与额度归属'),
        ('AgUserTokenQuotaReservation', 'RunId', 'uniqueidentifier', 16, 0, N'实际供应商请求所在运行，仅追踪不分账'),
        ('AgUserTokenQuotaReservation', 'DailyPeriodId', 'uniqueidentifier', 16, 1, N'原始日周期，未启用为空'),
        ('AgUserTokenQuotaReservation', 'MonthlyPeriodId', 'uniqueidentifier', 16, 1, N'原始月周期，未启用为空'),
        ('AgUserTokenQuotaReservation', 'ReservedTokens', 'bigint', 8, 0, N'本次固定预占 Token'),
        ('AgUserTokenQuotaReservation', 'ActualTokens', 'bigint', 8, 1, N'完整供应商报告，未知不记零'),
        ('AgUserTokenQuotaReservation', 'State', 'int', 4, 0, N'0 预占，1 开始，2 结算，3 未知，4 未开始释放'),
        ('AgUserTokenQuotaReservation', 'SettledAtUtc', 'datetime2', 8, 1, N'终态提交时间 UTC');
    IF EXISTS (SELECT 1 FROM @Expected e
        LEFT JOIN sys.columns c ON c.object_id = OBJECT_ID(N'dbo.' + e.TableName) AND c.name = e.ColumnName
        LEFT JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.column_id IS NULL OR t.name <> e.TypeName OR c.max_length <> e.MaxLength OR c.is_nullable <> e.Nullable)
        THROW 51992, N'Existing user Token quota schema is incompatible; no destructive conversion performed.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND i.is_primary_key=1 AND ic.key_ordinal=1 AND c.name=N'ID'
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1)
        THROW 51993, N'User Token quota primary key must be ID.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND name=N'index_AgUserTokenQuotaPeriod_Enabled')
        CREATE INDEX index_AgUserTokenQuotaPeriod_Enabled ON dbo.AgUserTokenQuotaPeriod (IsActive);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND i.name=N'index_AgUserTokenQuotaPeriod_Enabled' AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'IsActive'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND name=N'index_AgUserTokenQuotaPeriod_IsDeleted')
        CREATE INDEX index_AgUserTokenQuotaPeriod_IsDeleted ON dbo.AgUserTokenQuotaPeriod (IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND i.name=N'index_AgUserTokenQuotaPeriod_IsDeleted' AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'IsDeleted'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND name=N'UX_AgUserTokenQuotaPeriod_OwnerPeriod')
        CREATE UNIQUE INDEX UX_AgUserTokenQuotaPeriod_OwnerPeriod ON dbo.AgUserTokenQuotaPeriod (GroupId, CompanyId, PeriodKind, PeriodKey);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod') AND i.name=N'UX_AgUserTokenQuotaPeriod_OwnerPeriod' AND i.is_unique=1 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=4
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'GroupId')
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=2 AND c.name=N'CompanyId')
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=3 AND c.name=N'PeriodKind')
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=4 AND c.name=N'PeriodKey'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND i.is_primary_key=1 AND ic.key_ordinal=1 AND c.name=N'ID'
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1)
        THROW 51993, N'User Token quota primary key must be ID.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND name=N'index_AgUserTokenQuotaReservation_Enabled')
        CREATE INDEX index_AgUserTokenQuotaReservation_Enabled ON dbo.AgUserTokenQuotaReservation (IsActive);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND i.name=N'index_AgUserTokenQuotaReservation_Enabled' AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'IsActive'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND name=N'index_AgUserTokenQuotaReservation_IsDeleted')
        CREATE INDEX index_AgUserTokenQuotaReservation_IsDeleted ON dbo.AgUserTokenQuotaReservation (IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND i.name=N'index_AgUserTokenQuotaReservation_IsDeleted' AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=1
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'IsDeleted'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND name=N'IX_AgUserTokenQuotaReservation_OwnerState')
        CREATE INDEX IX_AgUserTokenQuotaReservation_OwnerState ON dbo.AgUserTokenQuotaReservation (GroupId, CompanyId, State);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND i.name=N'IX_AgUserTokenQuotaReservation_OwnerState' AND i.is_unique=0 AND i.is_disabled=0 AND i.has_filter=0
        AND (SELECT COUNT(*) FROM sys.index_columns k WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal>0)=3
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1 AND c.name=N'GroupId')
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=2 AND c.name=N'CompanyId')
        AND EXISTS (SELECT 1 FROM sys.index_columns k JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=3 AND c.name=N'State'))
        THROW 51994, N'Existing user Token quota index is incompatible.', 1;
    DECLARE @Table sysname, @Column sysname, @Description nvarchar(256);
    DECLARE quota_columns CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ColumnName, Description FROM @Expected;
    OPEN quota_columns;
    FETCH NEXT FROM quota_columns INTO @Table, @Column, @Description;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'dbo.' + @Table)
            AND minor_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.' + @Table), @Column, 'ColumnId') AND name=N'MS_Description')
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@Table, @level2type=N'COLUMN', @level2name=@Column;
        FETCH NEXT FROM quota_columns INTO @Table, @Column, @Description;
    END;
    CLOSE quota_columns;
    DEALLOCATE quota_columns;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
