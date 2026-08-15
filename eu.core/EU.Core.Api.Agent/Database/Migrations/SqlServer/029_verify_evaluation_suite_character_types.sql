-- Verify normalized Evaluation Suite tables contain VARCHAR only.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationSuite', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationSuiteVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationCase', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationCaseRule', N'U') IS NULL
    THROW 51529, N'Evaluation Suite normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'DocumentJson') IS NOT NULL
    THROW 51530, N'Evaluation Suite normalization is not finalized. Run the generated data script and Data/027 first.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.tables AS tableObject
    INNER JOIN sys.schemas AS schemaObject ON schemaObject.schema_id = tableObject.schema_id
    INNER JOIN sys.columns AS columnObject ON columnObject.object_id = tableObject.object_id
    INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
    WHERE schemaObject.name = N'dbo'
      AND tableObject.name IN (N'AgEvaluationSuite', N'AgEvaluationSuiteVersion', N'AgEvaluationCase', N'AgEvaluationCaseRule')
      AND typeObject.name IN (N'char', N'nchar', N'nvarchar'))
    THROW 51531, N'One or more Evaluation Suite character columns are not VARCHAR.', 1;

PRINT N'All normalized Evaluation Suite character columns use VARCHAR.';
GO
