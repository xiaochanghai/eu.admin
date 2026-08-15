-- Create normalized knowledge document and chunk tables. SQL Server 2014+.

SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgKnowledgeDocument', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgKnowledgeDocument (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgKnowledgeDocument_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgKnowledgeDocument_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL CONSTRAINT DF_AgKnowledgeDocument_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgKnowledgeDocument_Tag DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgKnowledgeDocument_AuditStatus DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            KnowledgeBaseId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL,
            FileName VARCHAR(512) NOT NULL,
            MediaType VARCHAR(128) NOT NULL,
            Sha256 VARCHAR(64) NOT NULL,
            Content VARCHAR(MAX) NOT NULL,
            ImportedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT pk_ag_knowledge_document PRIMARY KEY (ID),
            CONSTRAINT fk_ag_knowledge_document_base FOREIGN KEY (KnowledgeBaseId) REFERENCES dbo.AgKnowledgeBaseDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_knowledge_document_base_id UNIQUE (KnowledgeBaseId, ID),
            CONSTRAINT ux_ag_knowledge_document_order UNIQUE (KnowledgeBaseId, Ordinal),
            CONSTRAINT ck_ag_knowledge_document_ordinal CHECK (Ordinal >= 0),
            CONSTRAINT ck_ag_knowledge_document_sha256 CHECK (LEN(Sha256) = 64)
        );
        CREATE INDEX ix_ag_knowledge_document_base ON dbo.AgKnowledgeDocument(KnowledgeBaseId, Ordinal);
        CREATE INDEX index_AgKnowledgeDocument_IsDeleted ON dbo.AgKnowledgeDocument(IsDeleted);
    END;

    IF OBJECT_ID(N'dbo.AgKnowledgeChunk', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgKnowledgeChunk (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgKnowledgeChunk_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgKnowledgeChunk_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL CONSTRAINT DF_AgKnowledgeChunk_ModificationNum DEFAULT (0),
            Tag INT NULL CONSTRAINT DF_AgKnowledgeChunk_Tag DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgKnowledgeChunk_AuditStatus DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            KnowledgeBaseId UNIQUEIDENTIFIER NOT NULL,
            DocumentId UNIQUEIDENTIFIER NOT NULL,
            Sequence INT NOT NULL,
            Content VARCHAR(MAX) NOT NULL,
            CONSTRAINT pk_ag_knowledge_chunk PRIMARY KEY (ID),
            CONSTRAINT fk_ag_knowledge_chunk_base FOREIGN KEY (KnowledgeBaseId) REFERENCES dbo.AgKnowledgeBaseDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT fk_ag_knowledge_chunk_document_base FOREIGN KEY (KnowledgeBaseId, DocumentId)
                REFERENCES dbo.AgKnowledgeDocument(KnowledgeBaseId, ID),
            CONSTRAINT ux_ag_knowledge_chunk_sequence UNIQUE (DocumentId, Sequence),
            CONSTRAINT ck_ag_knowledge_chunk_sequence CHECK (Sequence >= 0)
        );
        CREATE INDEX ix_ag_knowledge_chunk_base ON dbo.AgKnowledgeChunk(KnowledgeBaseId, DocumentId, Sequence);
        CREATE INDEX index_AgKnowledgeChunk_IsDeleted ON dbo.AgKnowledgeChunk(IsDeleted);
    END;

    IF EXISTS (
        SELECT 1 FROM dbo.AgKnowledgeDocument
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), FileName))) <> CONVERT(VARBINARY(MAX), FileName)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), Content))) <> CONVERT(VARBINARY(MAX), Content)
           OR (CurrentNode IS NOT NULL AND
               CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), CurrentNode))) <> CONVERT(VARBINARY(MAX), CurrentNode)))
        THROW 51307, N'Knowledge document data contains characters that cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgKnowledgeChunk
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), Content))) <> CONVERT(VARBINARY(MAX), Content)
           OR (CurrentNode IS NOT NULL AND
               CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), CurrentNode))) <> CONVERT(VARBINARY(MAX), CurrentNode)))
        THROW 51308, N'Knowledge chunk data contains characters that cannot be represented by VARCHAR under the current database collation.', 1;

    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeDocument')
          AND columnObject.name = N'CurrentNode' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeDocument ALTER COLUMN CurrentNode VARCHAR(32) NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeDocument')
          AND columnObject.name = N'FileName' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeDocument ALTER COLUMN FileName VARCHAR(512) NOT NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeDocument')
          AND columnObject.name = N'Sha256' AND typeObject.name <> N'varchar')
        ALTER TABLE dbo.AgKnowledgeDocument ALTER COLUMN Sha256 VARCHAR(64) NOT NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeDocument')
          AND columnObject.name = N'Content' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeDocument ALTER COLUMN Content VARCHAR(MAX) NOT NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeChunk')
          AND columnObject.name = N'CurrentNode' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeChunk ALTER COLUMN CurrentNode VARCHAR(32) NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeChunk')
          AND columnObject.name = N'Content' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeChunk ALTER COLUMN Content VARCHAR(MAX) NOT NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgKnowledgeDocument')
          AND name = N'ux_ag_knowledge_document_base_id')
        CREATE UNIQUE INDEX ux_ag_knowledge_document_base_id
            ON dbo.AgKnowledgeDocument(KnowledgeBaseId, ID);

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgKnowledgeChunk')
          AND name = N'fk_ag_knowledge_chunk_document')
        ALTER TABLE dbo.AgKnowledgeChunk DROP CONSTRAINT fk_ag_knowledge_chunk_document;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgKnowledgeChunk')
          AND name = N'fk_ag_knowledge_chunk_document_base')
    BEGIN
        ALTER TABLE dbo.AgKnowledgeChunk WITH CHECK
            ADD CONSTRAINT fk_ag_knowledge_chunk_document_base
            FOREIGN KEY (KnowledgeBaseId, DocumentId)
            REFERENCES dbo.AgKnowledgeDocument(KnowledgeBaseId, ID);
        ALTER TABLE dbo.AgKnowledgeChunk
            CHECK CONSTRAINT fk_ag_knowledge_chunk_document_base;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
