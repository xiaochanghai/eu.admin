-- Finalize the normalized Skill schema without validating or rebuilding existing data.
-- Run 006 and 007 first. Stop EU.Core.Api.Agent and back up the database.
-- SQL Server 2014+.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgSkillDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgSkillVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgSkillVersionFile', N'U') IS NULL
    THROW 51160, N'Skill normalized tables are missing. Run 006 and 007 first.', 1;

IF COL_LENGTH(N'dbo.AgSkillDefinition', N'DocumentJson') IS NULL
    THROW 51161, N'DocumentJson is absent; the Skill cutover was already finalized.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Preserve every existing row and only fill nulls before enforcing NOT NULL.
    UPDATE dbo.AgSkillDefinition
    SET Name = COALESCE(Name, Code),
        Description = COALESCE(Description, N''),
        Category = COALESCE(Category, N''),
        Status = COALESCE(Status, 'Active');

    DECLARE @DropChecks NVARCHAR(MAX) = N'';
    SELECT @DropChecks = @DropChecks
        + N'ALTER TABLE dbo.AgSkillDefinition DROP CONSTRAINT '
        + QUOTENAME(name) + N';'
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
      AND definition LIKE N'%DocumentJson%';

    IF @DropChecks <> N''
        EXEC sys.sp_executesql @DropChecks;

    ALTER TABLE dbo.AgSkillDefinition ALTER COLUMN Name NVARCHAR(256) NOT NULL;
    ALTER TABLE dbo.AgSkillDefinition ALTER COLUMN Description NVARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgSkillDefinition ALTER COLUMN Category NVARCHAR(128) NOT NULL;
    ALTER TABLE dbo.AgSkillDefinition ALTER COLUMN Status VARCHAR(32) NOT NULL;

    IF OBJECT_ID(N'dbo.ck_ag_skill_definition_status', N'C') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD CONSTRAINT ck_ag_skill_definition_status
            CHECK (Status IN ('Active', 'Archived'));

    ALTER TABLE dbo.AgSkillDefinition DROP COLUMN DocumentJson;

    COMMIT TRANSACTION;
    PRINT N'Existing normalized Skill data was preserved and the Skill schema was finalized.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
