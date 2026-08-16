-- Normalize Agent API idempotency persistence for BasePoco and SqlSugar.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgApiIdempotency', N'U') IS NULL
    THROW 52100, N'dbo.AgApiIdempotency does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgApiIdempotency
        WHERE Status NOT IN ('InProgress', 'Completed', 'Indeterminate'))
        THROW 52101, N'AgApiIdempotency contains an unsupported status.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgApiIdempotency
        WHERE LEN(RTRIM(ScopeSha256)) <> 64 OR LEN(RTRIM(RequestSha256)) <> 64)
        THROW 52102, N'AgApiIdempotency contains an invalid SHA-256 value.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgApiIdempotency
        WHERE TRY_CONVERT(DATETIMEOFFSET(7), CreatedAtUtc, 127) IS NULL
           OR TRY_CONVERT(DATETIMEOFFSET(7), ExpiresAtUtc, 127) IS NULL)
        THROW 52103, N'AgApiIdempotency contains an invalid timestamp.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgApiIdempotency
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(256), ResponseContentType)))
              <> CONVERT(VARBINARY(MAX), ResponseContentType)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(2048), ResponseLocation)))
              <> CONVERT(VARBINARY(MAX), ResponseLocation))
        THROW 52104, N'AgApiIdempotency response metadata cannot be represented by VARCHAR.', 1;

    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'ID') IS NULL
    BEGIN
        ALTER TABLE dbo.AgApiIdempotency ADD ID UNIQUEIDENTIFIER NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgApiIdempotency SET ID = NEWID() WHERE ID IS NULL;
            ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN ID UNIQUEIDENTIFIER NOT NULL;';
    END;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'IsDeleted') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD IsDeleted BIT NOT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'IsActive') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD IsActive BIT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'ImportDataId') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'ModificationNum') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD ModificationNum INT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'Tag') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD Tag INT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'GroupId') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'CompanyId') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'AuditStatus') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD AuditStatus VARCHAR(32) NULL DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'CurrentNode') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'CreatedBy') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'CreatedTime') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'UpdateBy') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgApiIdempotency', N'UpdateTime') IS NULL ALTER TABLE dbo.AgApiIdempotency ADD UpdateTime DATETIME NULL;

    DECLARE @Sql NVARCHAR(MAX), @PkName SYSNAME;
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
          AND constraints.[type] = N'PK' AND columns.name = N'ID')
    BEGIN
        SELECT @PkName = name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND [type] = N'PK';
        IF @PkName IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgApiIdempotency DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_expires')
        DROP INDEX ix_ag_api_idempotency_expires ON dbo.AgApiIdempotency;

    DECLARE @CreatedAtType SYSNAME, @ExpiresAtType SYSNAME;
    SELECT @CreatedAtType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND columns.name = N'CreatedAtUtc';
    IF @CreatedAtType <> N'datetime2'
    BEGIN
        ALTER TABLE dbo.AgApiIdempotency ADD CreatedAtUtcValue DATETIME2(7) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgApiIdempotency
            SET CreatedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), CreatedAtUtc, 127));
            IF EXISTS (SELECT 1 FROM dbo.AgApiIdempotency WHERE CreatedAtUtcValue IS NULL)
                THROW 52105, N''AgApiIdempotency.CreatedAtUtc conversion failed.'', 1;';
        ALTER TABLE dbo.AgApiIdempotency DROP COLUMN CreatedAtUtc;
        EXEC sys.sp_rename N'dbo.AgApiIdempotency.CreatedAtUtcValue', N'CreatedAtUtc', N'COLUMN';
        ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN CreatedAtUtc DATETIME2(7) NOT NULL;
    END;

    SELECT @ExpiresAtType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND columns.name = N'ExpiresAtUtc';
    IF @ExpiresAtType <> N'datetime2'
    BEGIN
        ALTER TABLE dbo.AgApiIdempotency ADD ExpiresAtUtcValue DATETIME2(7) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgApiIdempotency
            SET ExpiresAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), ExpiresAtUtc, 127));
            IF EXISTS (SELECT 1 FROM dbo.AgApiIdempotency WHERE ExpiresAtUtcValue IS NULL)
                THROW 52106, N''AgApiIdempotency.ExpiresAtUtc conversion failed.'', 1;';
        ALTER TABLE dbo.AgApiIdempotency DROP COLUMN ExpiresAtUtc;
        EXEC sys.sp_rename N'dbo.AgApiIdempotency.ExpiresAtUtcValue', N'ExpiresAtUtc', N'COLUMN';
        ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN ExpiresAtUtc DATETIME2(7) NOT NULL;
    END;

    ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN ScopeSha256 VARCHAR(64) NOT NULL;
    ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN RequestSha256 VARCHAR(64) NOT NULL;
    ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN Status VARCHAR(32) NOT NULL;
    ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN ResponseContentType VARCHAR(256) NOT NULL;
    ALTER TABLE dbo.AgApiIdempotency ALTER COLUMN ResponseLocation VARCHAR(2048) NOT NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
          AND constraints.[type] = N'PK' AND columns.name = N'ID')
        ALTER TABLE dbo.AgApiIdempotency ADD CONSTRAINT pk_ag_api_idempotency_id PRIMARY KEY (ID);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ux_ag_api_idempotency_scope')
        CREATE UNIQUE INDEX ux_ag_api_idempotency_scope ON dbo.AgApiIdempotency(ScopeSha256);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_expires')
        CREATE INDEX ix_ag_api_idempotency_expires ON dbo.AgApiIdempotency(ExpiresAtUtc);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_is_deleted')
        CREATE INDEX ix_ag_api_idempotency_is_deleted ON dbo.AgApiIdempotency(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_is_active')
        CREATE INDEX ix_ag_api_idempotency_is_active ON dbo.AgApiIdempotency(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
