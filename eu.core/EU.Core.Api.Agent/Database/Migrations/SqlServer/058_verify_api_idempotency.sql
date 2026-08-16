-- Verify normalized Agent API idempotency persistence.
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgApiIdempotency', N'U') IS NULL
    THROW 52120, N'AgApiIdempotency is missing.', 1;
IF COL_LENGTH(N'dbo.AgApiIdempotency', N'ID') IS NULL
    THROW 52121, N'AgApiIdempotency.ID is missing.', 1;
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints constraints
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND constraints.[type] = N'PK'
      AND (SELECT COUNT(*) FROM sys.index_columns indexColumns
           WHERE indexColumns.object_id = constraints.parent_object_id
             AND indexColumns.index_id = constraints.unique_index_id
             AND indexColumns.key_ordinal > 0) = 1
      AND EXISTS (
          SELECT 1
          FROM sys.index_columns indexColumns
          INNER JOIN sys.columns columns
            ON columns.object_id = indexColumns.object_id
           AND columns.column_id = indexColumns.column_id
          WHERE indexColumns.object_id = constraints.parent_object_id
            AND indexColumns.index_id = constraints.unique_index_id
            AND indexColumns.key_ordinal = 1
            AND columns.name = N'ID'))
    THROW 52122, N'AgApiIdempotency.ID is not the primary key.', 1;
IF EXISTS (
    SELECT 1 FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND types.name IN (N'nchar', N'nvarchar', N'ntext', N'char'))
    THROW 52123, N'AgApiIdempotency contains a non-VARCHAR character column.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.AgApiIdempotency
    WHERE ScopeSha256 IS NULL OR RequestSha256 IS NULL OR Status IS NULL
       OR ResponseStatusCode IS NULL OR ResponseContentType IS NULL
       OR ResponseLocation IS NULL OR ResponseBody IS NULL
       OR CreatedAtUtc IS NULL OR ExpiresAtUtc IS NULL)
    THROW 52124, N'AgApiIdempotency contains incomplete rows.', 1;
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes indexes
    WHERE indexes.object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND indexes.name = N'ux_ag_api_idempotency_scope'
      AND indexes.is_unique = 1
      AND (SELECT COUNT(*) FROM sys.index_columns indexColumns
           WHERE indexColumns.object_id = indexes.object_id
             AND indexColumns.index_id = indexes.index_id
             AND indexColumns.key_ordinal > 0) = 1
      AND EXISTS (
          SELECT 1
          FROM sys.index_columns indexColumns
          INNER JOIN sys.columns columns
            ON columns.object_id = indexColumns.object_id
           AND columns.column_id = indexColumns.column_id
          WHERE indexColumns.object_id = indexes.object_id
            AND indexColumns.index_id = indexes.index_id
            AND indexColumns.key_ordinal = 1
            AND columns.name = N'ScopeSha256'))
    THROW 52125, N'AgApiIdempotency scope uniqueness is missing.', 1;
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes indexes
    INNER JOIN sys.index_columns indexColumns
      ON indexColumns.object_id = indexes.object_id
     AND indexColumns.index_id = indexes.index_id
     AND indexColumns.key_ordinal = 1
    INNER JOIN sys.columns columns
      ON columns.object_id = indexColumns.object_id
     AND columns.column_id = indexColumns.column_id
    WHERE indexes.object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND indexes.name = N'ix_ag_api_idempotency_expires'
      AND columns.name = N'ExpiresAtUtc')
    THROW 52126, N'AgApiIdempotency expiration index is missing.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.AgApiIdempotency
    WHERE Status NOT IN ('InProgress', 'Completed', 'Indeterminate'))
    THROW 52127, N'AgApiIdempotency contains an unsupported status.', 1;
IF EXISTS (
    SELECT expected.ColumnName, expected.TypeName, expected.MaxLength, expected.IsNullable
    FROM (VALUES
        (N'ID', N'uniqueidentifier', CONVERT(SMALLINT, 16), CONVERT(BIT, 0)),
        (N'ScopeSha256', N'varchar', CONVERT(SMALLINT, 64), CONVERT(BIT, 0)),
        (N'RequestSha256', N'varchar', CONVERT(SMALLINT, 64), CONVERT(BIT, 0)),
        (N'Status', N'varchar', CONVERT(SMALLINT, 32), CONVERT(BIT, 0)),
        (N'ResponseStatusCode', N'int', CONVERT(SMALLINT, 4), CONVERT(BIT, 0)),
        (N'ResponseContentType', N'varchar', CONVERT(SMALLINT, 256), CONVERT(BIT, 0)),
        (N'ResponseLocation', N'varchar', CONVERT(SMALLINT, 2048), CONVERT(BIT, 0)),
        (N'ResponseBody', N'varbinary', CONVERT(SMALLINT, -1), CONVERT(BIT, 0)),
        (N'CreatedAtUtc', N'datetime2', CONVERT(SMALLINT, 8), CONVERT(BIT, 0)),
        (N'ExpiresAtUtc', N'datetime2', CONVERT(SMALLINT, 8), CONVERT(BIT, 0)),
        (N'IsDeleted', N'bit', CONVERT(SMALLINT, 1), CONVERT(BIT, 0))
    ) expected(ColumnName, TypeName, MaxLength, IsNullable)
    EXCEPT
    SELECT columns.name, types.name, columns.max_length, columns.is_nullable
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgApiIdempotency'))
    THROW 52128, N'AgApiIdempotency contains an unexpected required column definition.', 1;
IF EXISTS (
    SELECT required.ColumnName
    FROM (VALUES
        (N'IsActive'), (N'ImportDataId'), (N'ModificationNum'), (N'Tag'),
        (N'GroupId'), (N'CompanyId'), (N'AuditStatus'), (N'CurrentNode'),
        (N'CreatedBy'), (N'CreatedTime'), (N'UpdateBy'), (N'UpdateTime')
    ) required(ColumnName)
    WHERE COL_LENGTH(N'dbo.AgApiIdempotency', required.ColumnName) IS NULL)
    THROW 52129, N'AgApiIdempotency is missing BasePoco columns.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_is_deleted')
    THROW 52130, N'AgApiIdempotency IsDeleted index is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_is_active')
    THROW 52131, N'AgApiIdempotency IsActive index is missing.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties
    WHERE major_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND minor_id = 0 AND name = N'MS_Description')
    THROW 52132, N'AgApiIdempotency table description is missing.', 1;
IF EXISTS (
    SELECT columns.name
    FROM sys.columns columns
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgApiIdempotency')
      AND NOT EXISTS (
          SELECT 1 FROM sys.extended_properties properties
          WHERE properties.major_id = columns.object_id
            AND properties.minor_id = columns.column_id
            AND properties.name = N'MS_Description'))
    THROW 52133, N'AgApiIdempotency contains a column without a description.', 1;

PRINT N'Agent API idempotency normalization verified.';
GO
