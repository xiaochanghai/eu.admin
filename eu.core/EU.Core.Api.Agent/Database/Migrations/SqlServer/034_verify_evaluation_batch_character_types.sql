-- Verify that normalized Evaluation Batch character columns use VARCHAR rather than NVARCHAR.

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'DocumentJson') IS NOT NULL
    THROW 51640, N'AgEvaluationBatch.DocumentJson still exists.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id IN (
        OBJECT_ID(N'dbo.AgEvaluationBatch'),
        OBJECT_ID(N'dbo.AgEvaluationBatchCase'),
        OBJECT_ID(N'dbo.AgEvaluationBatchCheck'),
        OBJECT_ID(N'dbo.AgEvaluationBatchObservation'))
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 51641, N'An Evaluation Batch character column still uses an NVARCHAR-family type.', 1;

IF EXISTS (
    SELECT required.TableName
    FROM (VALUES
        (N'AgEvaluationBatch'),
        (N'AgEvaluationBatchCase'),
        (N'AgEvaluationBatchCheck'),
        (N'AgEvaluationBatchObservation')) required(TableName)
    WHERE OBJECT_ID(N'dbo.' + required.TableName, N'U') IS NULL)
    THROW 51642, N'An Evaluation Batch normalized table is missing.', 1;

PRINT N'Evaluation Batch normalized character types verified.';
GO
