/*
P6 SQL Server deployment placeholder — intentionally not executed by the application.

The current standalone Host persists knowledge bases, source documents and generated
chunks in its local SQLite aggregate store. Before EU.Core.sln integration, translate
the following logical objects into the approved agent schema and SqlSugar conventions:

  agent.knowledge_base
  agent.knowledge_document
  agent.knowledge_chunk
  agent.agent_version_knowledge

Required operational decisions:
  - primary key/default/row-version conventions;
  - Unicode text and maximum source/chunk sizes;
  - indexes for code, status, document and chunk sequence;
  - retention and soft-delete policy;
  - whether lexical retrieval remains in-process or uses an approved search service.

No SQL statements are included until the owner confirms EU.Core SQL Server and
operations conventions. Database creation and migration remain manual.
*/
