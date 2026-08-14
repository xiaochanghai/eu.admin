-- Create normalized Skill published-version and file-manifest tables.
-- Prerequisite: 006_add_basepoco_and_fields_ag_skill_definition.sql.
-- SQL Server 2014+

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgSkillDefinition', N'U') IS NULL
    THROW 51070, N'dbo.AgSkillDefinition does not exist. Run 001 and 006 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgSkillVersion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgSkillVersion (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgSkillVersion_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgSkillVersion_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT DF_AgSkillVersion_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgSkillVersion_Tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgSkillVersion_AuditStatus DEFAULT ('Add'),
            CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            SkillId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL,
            Label NVARCHAR(128) NOT NULL,
            ManifestSha256 CHAR(64) NOT NULL,
            PublishedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT pk_ag_skill_version PRIMARY KEY (ID),
            CONSTRAINT fk_ag_skill_version_definition FOREIGN KEY (SkillId)
                REFERENCES dbo.AgSkillDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_skill_version_order UNIQUE (SkillId, Ordinal),
            CONSTRAINT ux_ag_skill_version_label UNIQUE (SkillId, Label),
            CONSTRAINT ck_ag_skill_version_ordinal CHECK (Ordinal >= 0),
            CONSTRAINT ck_ag_skill_version_manifest_sha256 CHECK (LEN(ManifestSha256) = 64)
        );
        CREATE INDEX ix_ag_skill_version_skill ON dbo.AgSkillVersion(SkillId, Ordinal);
        CREATE INDEX index_AgSkillVersion_Enabled ON dbo.AgSkillVersion(IsActive);
        CREATE INDEX index_AgSkillVersion_IsDeleted ON dbo.AgSkillVersion(IsDeleted);
    END;

    IF OBJECT_ID(N'dbo.AgSkillVersionFile', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgSkillVersionFile (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgSkillVersionFile_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgSkillVersionFile_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT DF_AgSkillVersionFile_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgSkillVersionFile_Tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgSkillVersionFile_AuditStatus DEFAULT ('Add'),
            CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            VersionId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL,
            [Path] NVARCHAR(1024) NOT NULL,
            Size BIGINT NOT NULL,
            Sha256 CHAR(64) NOT NULL,
            CONSTRAINT pk_ag_skill_version_file PRIMARY KEY (ID),
            CONSTRAINT fk_ag_skill_version_file_version FOREIGN KEY (VersionId)
                REFERENCES dbo.AgSkillVersion(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_skill_version_file_order UNIQUE (VersionId, Ordinal),
            CONSTRAINT ck_ag_skill_version_file_ordinal CHECK (Ordinal >= 0),
            CONSTRAINT ck_ag_skill_version_file_size CHECK (Size >= 0),
            CONSTRAINT ck_ag_skill_version_file_sha256 CHECK (LEN(Sha256) = 64)
        );
        CREATE INDEX ix_ag_skill_version_file_version ON dbo.AgSkillVersionFile(VersionId, Ordinal);
        CREATE INDEX index_AgSkillVersionFile_Enabled ON dbo.AgSkillVersionFile(IsActive);
        CREATE INDEX index_AgSkillVersionFile_IsDeleted ON dbo.AgSkillVersionFile(IsDeleted);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Normalized Skill version tables are ready; migrate DocumentJson before starting the normalized API.';
