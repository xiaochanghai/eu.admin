-- Verify normalized Unified Entry persistence.
SET NOCOUNT ON;

DECLARE @Tables TABLE(TableName SYSNAME,IndexStem SYSNAME);
INSERT @Tables VALUES
(N'AgChatConversation',N'chat_conversation'),(N'AgChatMessage',N'chat_message'),(N'AgUnifiedEntryRun',N'unified_entry_run'),
(N'AgUnifiedAgentRun',N'unified_agent_run'),(N'AgUnifiedOrchestrationLink',N'unified_orchestration_link'),(N'AgUnifiedToolCall',N'unified_tool_call'),(N'AgUnifiedRunEvent',N'unified_run_event');

IF EXISTS(SELECT 1 FROM @Tables WHERE OBJECT_ID(N'dbo.'+TableName,N'U') IS NULL)
    THROW 52330,N'A normalized Unified Entry table is missing.',1;
IF EXISTS(SELECT 1 FROM sys.columns columns JOIN sys.types types ON types.user_type_id=columns.user_type_id WHERE columns.object_id IN
    (OBJECT_ID(N'dbo.AgChatConversation'),OBJECT_ID(N'dbo.AgChatMessage'),OBJECT_ID(N'dbo.AgUnifiedEntryRun'),OBJECT_ID(N'dbo.AgUnifiedAgentRun'),OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink'),OBJECT_ID(N'dbo.AgUnifiedToolCall'),OBJECT_ID(N'dbo.AgUnifiedRunEvent'))
    AND types.name IN(N'nchar',N'nvarchar',N'ntext',N'char'))
    THROW 52331,N'Unified Entry tables contain a non-VARCHAR character column.',1;
IF EXISTS(SELECT 1 FROM @Tables tables WHERE NOT EXISTS(SELECT 1 FROM sys.key_constraints constraints WHERE constraints.parent_object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND constraints.type=N'PK'))
    THROW 52332,N'A Unified Entry primary key is missing.',1;
IF EXISTS(SELECT 1 FROM @Tables tables JOIN sys.columns columns ON columns.object_id=OBJECT_ID(N'dbo.'+tables.TableName) WHERE NOT EXISTS(SELECT 1 FROM sys.extended_properties properties WHERE properties.major_id=columns.object_id AND properties.minor_id=columns.column_id AND properties.name=N'MS_Description'))
    THROW 52333,N'A Unified Entry column description is missing.',1;
IF EXISTS(SELECT 1 FROM @Tables tables WHERE NOT EXISTS(SELECT 1 FROM sys.extended_properties properties WHERE properties.major_id=OBJECT_ID(N'dbo.'+tables.TableName) AND properties.minor_id=0 AND properties.name=N'MS_Description'))
    THROW 52334,N'A Unified Entry table description is missing.',1;
IF EXISTS(SELECT 1 FROM @Tables tables WHERE
    NOT EXISTS(SELECT 1 FROM sys.indexes indexes WHERE indexes.object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND indexes.name=N'ix_ag_'+tables.IndexStem+N'_is_deleted') OR
    NOT EXISTS(SELECT 1 FROM sys.indexes indexes WHERE indexes.object_id=OBJECT_ID(N'dbo.'+tables.TableName) AND indexes.name=N'ix_ag_'+tables.IndexStem+N'_is_active'))
    THROW 52335,N'A Unified Entry BasePoco index is missing.',1;

IF EXISTS(SELECT 1 FROM dbo.AgChatMessage child LEFT JOIN dbo.AgChatConversation parent ON parent.ID=child.ConversationId WHERE parent.ID IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedEntryRun child LEFT JOIN dbo.AgChatConversation parent ON parent.ID=child.ConversationId WHERE parent.ID IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedAgentRun child LEFT JOIN dbo.AgUnifiedEntryRun parent ON parent.ID=child.EntryRunId WHERE parent.ID IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedOrchestrationLink child LEFT JOIN dbo.AgUnifiedEntryRun parent ON parent.ID=child.EntryRunId WHERE parent.ID IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedToolCall child LEFT JOIN dbo.AgUnifiedEntryRun parent ON parent.ID=child.EntryRunId WHERE parent.ID IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedRunEvent child LEFT JOIN dbo.AgUnifiedEntryRun parent ON parent.ID=child.EntryRunId WHERE parent.ID IS NULL)
    THROW 52336,N'Unified Entry tables contain orphan rows.',1;
IF EXISTS(SELECT 1 FROM dbo.AgChatMessage WHERE Ordinal<0 OR ContentUtf8Bytes<0)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedEntryRun WHERE PersistenceRevision<0)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedAgentRun WHERE Ordinal<0 OR Depth<0)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedOrchestrationLink WHERE Ordinal<0 OR Depth<0)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedToolCall WHERE Ordinal<0 OR Depth<0)
 OR EXISTS(SELECT 1 FROM dbo.AgUnifiedRunEvent WHERE Sequence<=0 OR Depth<0)
    THROW 52337,N'Unified Entry state data is invalid.',1;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'ux_ag_chat_message_conversation_ordinal' AND is_unique=1)
 OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND name=N'ux_ag_unified_agent_run_entry_ordinal' AND is_unique=1)
 OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND name=N'ux_ag_unified_orchestration_link_entry_ordinal' AND is_unique=1)
 OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND name=N'ux_ag_unified_tool_call_entry_ordinal' AND is_unique=1)
 OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND name=N'ux_ag_unified_run_event_entry_sequence' AND is_unique=1)
    THROW 52338,N'Unified Entry uniqueness is missing.',1;
IF (SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE name IN(N'fk_ag_chat_message_conversation',N'fk_ag_unified_entry_run_conversation',N'fk_ag_unified_agent_run_entry',N'fk_ag_unified_orchestration_link_entry',N'fk_ag_unified_tool_call_entry',N'fk_ag_unified_run_event_entry'))<>6
    THROW 52339,N'A Unified Entry foreign key is missing.',1;
IF (SELECT COUNT_BIG(*) FROM sys.check_constraints WHERE name IN(N'ck_ag_chat_message_ordinal',N'ck_ag_chat_message_content_bytes',N'ck_ag_unified_entry_run_revision',N'ck_ag_unified_agent_run_ordinal',N'ck_ag_unified_agent_run_depth',N'ck_ag_unified_orchestration_link_ordinal',N'ck_ag_unified_orchestration_link_depth',N'ck_ag_unified_tool_call_ordinal',N'ck_ag_unified_tool_call_depth',N'ck_ag_unified_run_event_sequence',N'ck_ag_unified_run_event_depth'))<>11
    THROW 52340,N'A Unified Entry check constraint is missing.',1;
IF EXISTS(SELECT 1 FROM (VALUES
 (N'AgChatConversation',N'ID',N'uniqueidentifier'),(N'AgChatMessage',N'ConversationId',N'uniqueidentifier'),(N'AgUnifiedEntryRun',N'ID',N'uniqueidentifier'),
 (N'AgUnifiedAgentRun',N'EntryRunId',N'uniqueidentifier'),(N'AgUnifiedOrchestrationLink',N'EntryRunId',N'uniqueidentifier'),(N'AgUnifiedToolCall',N'EntryRunId',N'uniqueidentifier'),(N'AgUnifiedRunEvent',N'EntryRunId',N'uniqueidentifier'),
 (N'AgChatConversation',N'CreatedAtUtc',N'datetime2'),(N'AgChatMessage',N'CreatedAtUtc',N'datetime2'),(N'AgUnifiedEntryRun',N'StartedAtUtc',N'datetime2'),(N'AgUnifiedAgentRun',N'StartedAtUtc',N'datetime2'),(N'AgUnifiedOrchestrationLink',N'StartedAtUtc',N'datetime2'),(N'AgUnifiedToolCall',N'StartedAtUtc',N'datetime2'),(N'AgUnifiedRunEvent',N'OccurredAtUtc',N'datetime2')) expected(TableName,ColumnName,TypeName)
 LEFT JOIN sys.columns columns ON columns.object_id=OBJECT_ID(N'dbo.'+expected.TableName) AND columns.name=expected.ColumnName
 LEFT JOIN sys.types types ON types.user_type_id=columns.user_type_id WHERE types.name IS NULL OR types.name<>expected.TypeName)
    THROW 52341,N'A Unified Entry key or timestamp column has an invalid type.',1;

PRINT N'Unified Entry normalization verified.';
GO
