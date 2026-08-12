-- Prepare relational Agent detail tables for SQL Server 2014.
-- Prerequisite: 001_initial_schema.sql and 002_add_basepoco_columns_ag_agent_definition.sql.
-- This script intentionally does not parse or drop DocumentJson. SQL Server 2014
-- has no native JSON parser; load pre-expanded detail INSERT statements separately.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentDefinition', N'U') IS NULL
    THROW 51030, N'dbo.AgAgentDefinition does not exist. Run 001 and 002 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'Name') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD Name NVARCHAR(256) NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'Description') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD Description NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'RuntimeStatus') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD RuntimeStatus VARCHAR(32) NULL;

    IF OBJECT_ID(N'dbo.AgAgentVersion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgAgentVersion (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgAgentVersion_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgAgentVersion_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT DF_AgAgentVersion_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgAgentVersion_Tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgAgentVersion_AuditStatus DEFAULT ('Add'),
            CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            AgentId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL,
            Label NVARCHAR(128) NOT NULL,
            IsDraft BIT NOT NULL,
            Instructions NVARCHAR(MAX) NOT NULL,
            ModelProfileId NVARCHAR(256) NOT NULL,
            OutputMode VARCHAR(32) NOT NULL,
            OutputJsonSchema NVARCHAR(MAX) NULL,
            OutputSchemaSha256 CHAR(64) NULL,
            CONSTRAINT pk_ag_agent_version PRIMARY KEY (ID),
            CONSTRAINT fk_ag_agent_version_definition FOREIGN KEY (AgentId)
                REFERENCES dbo.AgAgentDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_agent_version_order UNIQUE (AgentId, IsDraft, Ordinal),
            CONSTRAINT ck_ag_agent_version_draft_ordinal CHECK (IsDraft = 0 OR Ordinal = 0),
            CONSTRAINT ck_ag_agent_version_ordinal CHECK (Ordinal >= 0),
            CONSTRAINT ck_ag_agent_version_output_mode CHECK (OutputMode IN ('Text', 'Structured'))
        );
        CREATE INDEX ix_ag_agent_version_agent ON dbo.AgAgentVersion(AgentId, IsDraft, Ordinal);
        CREATE INDEX index_AgAgentVersion_Enabled ON dbo.AgAgentVersion(IsActive);
        CREATE INDEX index_AgAgentVersion_IsDeleted ON dbo.AgAgentVersion(IsDeleted);
        CREATE UNIQUE INDEX ux_ag_agent_version_single_draft
            ON dbo.AgAgentVersion(AgentId) WHERE IsDraft = 1;
    END;

    IF OBJECT_ID(N'dbo.AgAgentVersionSnapshot', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgAgentVersionSnapshot (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgAgentVersionSnapshot_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgAgentVersionSnapshot_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT DF_AgAgentVersionSnapshot_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgAgentVersionSnapshot_Tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgAgentVersionSnapshot_AuditStatus DEFAULT ('Add'),
            CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            VersionId UNIQUEIDENTIFIER NOT NULL,
            SnapshotVersionId UNIQUEIDENTIFIER NOT NULL,
            AgentCode NVARCHAR(128) NOT NULL,
            Instructions NVARCHAR(MAX) NOT NULL,
            ModelProfileId NVARCHAR(256) NOT NULL,
            OutputMode VARCHAR(32) NOT NULL,
            OutputJsonSchema NVARCHAR(MAX) NULL,
            AgentName NVARCHAR(256) NULL,
            AgentDescription NVARCHAR(MAX) NULL,
            CONSTRAINT pk_ag_agent_version_snapshot PRIMARY KEY (ID),
            CONSTRAINT fk_ag_agent_version_snapshot_version FOREIGN KEY (VersionId)
                REFERENCES dbo.AgAgentVersion(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_agent_version_snapshot_version UNIQUE (VersionId),
            CONSTRAINT ck_ag_agent_snapshot_output_mode CHECK (OutputMode IN ('Text', 'Structured'))
        );
        CREATE INDEX index_AgAgentVersionSnapshot_Enabled ON dbo.AgAgentVersionSnapshot(IsActive);
        CREATE INDEX index_AgAgentVersionSnapshot_IsDeleted ON dbo.AgAgentVersionSnapshot(IsDeleted);
    END;

    IF OBJECT_ID(N'dbo.AgAgentVersionBinding', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgAgentVersionBinding (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgAgentVersionBinding_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgAgentVersionBinding_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT DF_AgAgentVersionBinding_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgAgentVersionBinding_Tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgAgentVersionBinding_AuditStatus DEFAULT ('Add'),
            CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            VersionId UNIQUEIDENTIFIER NOT NULL,
            Scope VARCHAR(16) NOT NULL,
            BindingType VARCHAR(32) NOT NULL,
            Ordinal INT NOT NULL,
            ReferenceId UNIQUEIDENTIFIER NOT NULL,
            ReferenceVersionId UNIQUEIDENTIFIER NULL,
            LogicalRevision BIGINT NULL,
            ReferenceCode NVARCHAR(128) NULL,
            ReferenceName NVARCHAR(256) NULL,
            ReferenceDescription NVARCHAR(MAX) NULL,
            CONSTRAINT pk_ag_agent_version_binding PRIMARY KEY (ID),
            CONSTRAINT fk_ag_agent_version_binding_version FOREIGN KEY (VersionId)
                REFERENCES dbo.AgAgentVersion(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_agent_version_binding_order UNIQUE (VersionId, Scope, BindingType, Ordinal),
            CONSTRAINT ck_ag_agent_binding_scope CHECK (Scope IN ('Version', 'Snapshot')),
            CONSTRAINT ck_ag_agent_binding_type CHECK (
                BindingType IN ('Skill', 'Tool', 'KnowledgeBase', 'ChildAgent', 'Orchestration')),
            CONSTRAINT ck_ag_agent_binding_ordinal CHECK (Ordinal >= 0),
            CONSTRAINT ck_ag_agent_binding_revision CHECK (LogicalRevision IS NULL OR LogicalRevision >= 0)
        );
        CREATE INDEX index_AgAgentVersionBinding_Enabled ON dbo.AgAgentVersionBinding(IsActive);
        CREATE INDEX index_AgAgentVersionBinding_IsDeleted ON dbo.AgAgentVersionBinding(IsDeleted);
        CREATE INDEX ix_ag_agent_binding_reference ON dbo.AgAgentVersionBinding
            (BindingType, ReferenceId, ReferenceVersionId);
        CREATE UNIQUE INDEX ux_ag_agent_binding_unique_reference ON dbo.AgAgentVersionBinding
            (VersionId, Scope, BindingType, ReferenceId);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Agent normalized schema is ready. Load pre-expanded Agent detail rows before starting the normalized API.';
