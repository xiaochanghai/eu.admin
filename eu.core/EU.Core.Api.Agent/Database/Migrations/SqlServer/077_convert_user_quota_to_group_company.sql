-- 将未发布的旧 TenantId / GroupId+CompanyId+UserId 额度预览结构转换为 GroupId+CompanyId 所有权。
-- 仅允许四张额度表全部为空时执行；已有账本必须人工制定合并与对账方案，脚本不会推断或删除数据。
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (
 OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaReservation'),
 OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name IN (N'TenantId', N'UserId'))
BEGIN
 DECLARE @Tables TABLE (TableName sysname PRIMARY KEY, OwnerIndex sysname, UniqueOwner bit, NewColumns nvarchar(256));
 INSERT INTO @Tables VALUES
  (N'AgUserTokenQuotaPeriod', N'UX_AgUserTokenQuotaPeriod_OwnerPeriod', 1, N'[GroupId], [CompanyId], [PeriodKind], [PeriodKey]'),
  (N'AgUserTokenQuotaReservation', N'IX_AgUserTokenQuotaReservation_OwnerState', 0, N'[GroupId], [CompanyId], [State]'),
  (N'AgUserTokenQuotaPolicy', N'UX_AgUserTokenQuotaPolicy_Owner', 1, N'[GroupId], [CompanyId]'),
  (N'AgUserTokenQuotaAdjustment', N'IX_AgUserTokenQuotaAdjustment_Owner', 0, N'[GroupId], [CompanyId]');

 DECLARE @Table sysname, @Sql nvarchar(max), @Rows bigint, @Index sysname, @Unique bit, @Columns nvarchar(256);
 DECLARE empty_check CURSOR LOCAL FAST_FORWARD FOR
  SELECT TableName FROM @Tables WHERE OBJECT_ID(N'dbo.' + TableName, N'U') IS NOT NULL ORDER BY TableName;
 OPEN empty_check;
 FETCH NEXT FROM empty_check INTO @Table;
 WHILE @@FETCH_STATUS=0
 BEGIN
  SET @Sql=N'SELECT @Value=COUNT_BIG(*) FROM dbo.' + QUOTENAME(@Table) + N' WITH (TABLOCKX, HOLDLOCK);';
  EXEC sys.sp_executesql @Sql, N'@Value bigint OUTPUT', @Value=@Rows OUTPUT;
  IF @Rows<>0 THROW 51995, 'Legacy per-user quota schema contains data; stop and reconcile ownership before upgrading. No conversion performed.', 1;
  FETCH NEXT FROM empty_check INTO @Table;
 END;
 CLOSE empty_check;
 DEALLOCATE empty_check;

 IF EXISTS (SELECT 1 FROM @Tables q JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.' + q.TableName) AND c.name IN (N'TenantId', N'UserId')
  JOIN sys.index_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id
  JOIN sys.indexes i ON i.object_id=ic.object_id AND i.index_id=ic.index_id
  WHERE i.name<>q.OwnerIndex)
  THROW 51997, 'Unrecognized legacy quota index; no conversion performed.', 1;
 IF EXISTS (SELECT 1 FROM sys.foreign_keys f WHERE f.parent_object_id IN (SELECT OBJECT_ID(N'dbo.' + TableName) FROM @Tables)
      OR f.referenced_object_id IN (SELECT OBJECT_ID(N'dbo.' + TableName) FROM @Tables))
  OR EXISTS (SELECT 1 FROM sys.triggers tr WHERE tr.parent_id IN (SELECT OBJECT_ID(N'dbo.' + TableName) FROM @Tables))
  OR EXISTS (SELECT 1 FROM sys.check_constraints cc WHERE cc.parent_object_id IN (SELECT OBJECT_ID(N'dbo.' + TableName) FROM @Tables))
  THROW 51998, 'Custom quota dependencies require manual review; no conversion performed.', 1;

 DECLARE convert_tables CURSOR LOCAL FAST_FORWARD FOR
  SELECT TableName, OwnerIndex, UniqueOwner, NewColumns FROM @Tables WHERE OBJECT_ID(N'dbo.' + TableName, N'U') IS NOT NULL ORDER BY TableName;
 OPEN convert_tables;
 FETCH NEXT FROM convert_tables INTO @Table, @Index, @Unique, @Columns;
 WHILE @@FETCH_STATUS=0
 BEGIN
  IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.' + @Table) AND name=@Index)
  BEGIN
   SET @Sql=N'DROP INDEX ' + QUOTENAME(@Index) + N' ON dbo.' + QUOTENAME(@Table) + N';';
   EXEC sys.sp_executesql @Sql;
  END;

  DECLARE @Column sysname, @Default sysname;
  DECLARE legacy_columns CURSOR LOCAL FAST_FORWARD FOR
   SELECT c.name, dc.name FROM sys.columns c LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
   WHERE c.object_id=OBJECT_ID(N'dbo.' + @Table) AND c.name IN (N'TenantId', N'UserId') ORDER BY c.column_id;
  OPEN legacy_columns;
  FETCH NEXT FROM legacy_columns INTO @Column, @Default;
  WHILE @@FETCH_STATUS=0
  BEGIN
   IF @Default IS NOT NULL
   BEGIN
    SET @Sql=N'ALTER TABLE dbo.' + QUOTENAME(@Table) + N' DROP CONSTRAINT ' + QUOTENAME(@Default) + N';';
    EXEC sys.sp_executesql @Sql;
   END;
   IF @Table=N'AgUserTokenQuotaReservation' AND @Column=N'UserId'
   BEGIN
    IF COL_LENGTH(N'dbo.AgUserTokenQuotaReservation', N'ConsumerUserId') IS NOT NULL
     THROW 51996, 'Reservation contains both UserId and ConsumerUserId; no conversion performed.', 1;
    EXEC sys.sp_rename N'dbo.AgUserTokenQuotaReservation.UserId', N'ConsumerUserId', N'COLUMN';
   END
   ELSE
   BEGIN
    SET @Sql=N'ALTER TABLE dbo.' + QUOTENAME(@Table) + N' DROP COLUMN ' + QUOTENAME(@Column) + N';';
    EXEC sys.sp_executesql @Sql;
   END;
   FETCH NEXT FROM legacy_columns INTO @Column, @Default;
  END;
  CLOSE legacy_columns;
  DEALLOCATE legacy_columns;

  SET @Sql=N'CREATE ' + CASE WHEN @Unique=1 THEN N'UNIQUE ' ELSE N'' END + N'INDEX ' + QUOTENAME(@Index)
   + N' ON dbo.' + QUOTENAME(@Table) + N' (' + @Columns + N');';
  EXEC sys.sp_executesql @Sql;
  FETCH NEXT FROM convert_tables INTO @Table, @Index, @Unique, @Columns;
 END;
 CLOSE convert_tables;
 DEALLOCATE convert_tables;

 IF OBJECT_ID(N'dbo.AgUserTokenQuotaReservation', N'U') IS NOT NULL AND
  NOT EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
   WHERE c.object_id=OBJECT_ID(N'dbo.AgUserTokenQuotaReservation') AND c.name=N'ConsumerUserId'
    AND t.name=N'uniqueidentifier' AND c.is_nullable=0)
  THROW 51996, 'Reservation consumer user column is incompatible; conversion rolled back.', 1;
 IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (
  OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaReservation'),
  OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'TenantId')
  OR EXISTS (SELECT 1 FROM sys.columns WHERE object_id IN (
  OBJECT_ID(N'dbo.AgUserTokenQuotaPeriod'), OBJECT_ID(N'dbo.AgUserTokenQuotaPolicy'), OBJECT_ID(N'dbo.AgUserTokenQuotaAdjustment')) AND name=N'UserId')
  THROW 51996, 'Legacy quota owner columns remain; conversion rolled back.', 1;
END;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF CURSOR_STATUS('local', 'legacy_columns')>-1 CLOSE legacy_columns;
 IF CURSOR_STATUS('local', 'legacy_columns')>=-1 DEALLOCATE legacy_columns;
 IF CURSOR_STATUS('local', 'convert_tables')>-1 CLOSE convert_tables;
 IF CURSOR_STATUS('local', 'convert_tables')>=-1 DEALLOCATE convert_tables;
 IF CURSOR_STATUS('local', 'empty_check')>-1 CLOSE empty_check;
 IF CURSOR_STATUS('local', 'empty_check')>=-1 DEALLOCATE empty_check;
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
