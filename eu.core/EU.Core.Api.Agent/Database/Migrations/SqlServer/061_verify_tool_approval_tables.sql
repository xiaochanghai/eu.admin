-- Verify normalized Tool Approval persistence.
SET NOCOUNT ON;

DECLARE @Tables TABLE (TableName SYSNAME);
INSERT @Tables VALUES
(N'AgToolApprovalRequest'),(N'AgToolApprovalPayload'),(N'AgToolApprovalDecision'),(N'AgToolApprovalExecutionResult');

IF EXISTS (SELECT 1 FROM @Tables WHERE OBJECT_ID(N'dbo.'+TableName,N'U') IS NULL)
    THROW 52230,N'A normalized Tool Approval table is missing.',1;
IF EXISTS (
    SELECT 1 FROM sys.columns columns JOIN sys.types types ON types.user_type_id=columns.user_type_id
    WHERE columns.object_id IN (OBJECT_ID(N'dbo.AgToolApprovalRequest'),OBJECT_ID(N'dbo.AgToolApprovalPayload'),OBJECT_ID(N'dbo.AgToolApprovalDecision'),OBJECT_ID(N'dbo.AgToolApprovalExecutionResult'))
      AND types.name IN (N'nchar',N'nvarchar',N'ntext',N'char'))
    THROW 52231,N'Tool Approval tables contain a non-VARCHAR character column.',1;
IF EXISTS (
    SELECT 1 FROM @Tables tables
    WHERE NOT EXISTS (SELECT 1 FROM sys.key_constraints constraints WHERE constraints.parent_object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND constraints.[type]=N'PK'))
    THROW 52232,N'A Tool Approval primary key is missing.',1;
IF EXISTS (
    SELECT 1 FROM @Tables tables JOIN sys.columns columns ON columns.object_id=OBJECT_ID(N'dbo.'+tables.TableName)
    WHERE NOT EXISTS (SELECT 1 FROM sys.extended_properties properties WHERE properties.major_id=columns.object_id AND properties.minor_id=columns.column_id AND properties.name=N'MS_Description'))
    THROW 52233,N'A Tool Approval column description is missing.',1;
IF EXISTS (
    SELECT 1 FROM @Tables tables
    WHERE NOT EXISTS (SELECT 1 FROM sys.indexes indexes WHERE indexes.object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND indexes.name=N'ix_ag_'+LOWER(REPLACE(tables.TableName,N'AgToolApproval',N'tool_approval_'))+N'_is_deleted')
       OR NOT EXISTS (SELECT 1 FROM sys.indexes indexes WHERE indexes.object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND indexes.name=N'ix_ag_'+LOWER(REPLACE(tables.TableName,N'AgToolApproval',N'tool_approval_'))+N'_is_active'))
    THROW 52237,N'A Tool Approval BasePoco index is missing.',1;
IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalPayload payload LEFT JOIN dbo.AgToolApprovalRequest request ON request.ID=payload.ApprovalId WHERE request.ID IS NULL)
   OR EXISTS (SELECT 1 FROM dbo.AgToolApprovalDecision decision LEFT JOIN dbo.AgToolApprovalRequest request ON request.ID=decision.ApprovalId WHERE request.ID IS NULL)
   OR EXISTS (SELECT 1 FROM dbo.AgToolApprovalExecutionResult result LEFT JOIN dbo.AgToolApprovalRequest request ON request.ID=result.ApprovalId WHERE request.ID IS NULL)
    THROW 52234,N'Tool Approval tables contain orphan rows.',1;
IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalRequest WHERE LogicalRevision<0 OR Status NOT BETWEEN 0 AND 8 OR Risk NOT IN (2,3))
    THROW 52235,N'Tool Approval state data is invalid.',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalPayload') AND name=N'ux_ag_tool_approval_payload_approval' AND is_unique=1)
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalDecision') AND name=N'ux_ag_tool_approval_decision_revision' AND is_unique=1)
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalExecutionResult') AND name=N'ux_ag_tool_approval_execution_result_approval' AND is_unique=1)
    THROW 52236,N'Tool Approval uniqueness is missing.',1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalRequest') AND name=N'ck_ag_tool_approval_request_revision')
    THROW 52238,N'Tool Approval logical revision constraint is missing.',1;
IF EXISTS (
    SELECT 1
    FROM (VALUES
        (N'AgToolApprovalRequest',N'ID',N'uniqueidentifier'),
        (N'AgToolApprovalPayload',N'ApprovalId',N'uniqueidentifier'),
        (N'AgToolApprovalDecision',N'ApprovalId',N'uniqueidentifier'),
        (N'AgToolApprovalExecutionResult',N'ApprovalId',N'uniqueidentifier'),
        (N'AgToolApprovalRequest',N'RequestedAtUtc',N'datetime2'),
        (N'AgToolApprovalRequest',N'ExpiresAtUtc',N'datetime2'),
        (N'AgToolApprovalDecision',N'DecidedAtUtc',N'datetime2'),
        (N'AgToolApprovalExecutionResult',N'FinishedAtUtc',N'datetime2')) expected(TableName,ColumnName,TypeName)
    LEFT JOIN sys.columns columns ON columns.object_id=OBJECT_ID(N'dbo.'+expected.TableName) AND columns.name=expected.ColumnName
    LEFT JOIN sys.types types ON types.user_type_id=columns.user_type_id
    WHERE types.name IS NULL OR types.name<>expected.TypeName)
    THROW 52239,N'A Tool Approval key or timestamp column has an invalid type.',1;

PRINT N'Tool Approval normalization verified.';
GO
