-- Verify normalized Evaluation Model Judgement tables and character types.

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'DocumentJson') IS NOT NULL
    THROW 51740, N'AgEvaluationModelJudgement.DocumentJson still exists.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id IN (
        OBJECT_ID(N'dbo.AgEvaluationModelJudgement'),
        OBJECT_ID(N'dbo.AgEvaluationModelJudgementEvaluator'),
        OBJECT_ID(N'dbo.AgEvaluationModelJudgementMinimumScore'),
        OBJECT_ID(N'dbo.AgEvaluationModelJudgementCase'),
        OBJECT_ID(N'dbo.AgEvaluationModelJudgementMetric'),
        OBJECT_ID(N'dbo.AgEvaluationModelJudgementDiagnostic'))
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 51741, N'An Evaluation Model Judgement character column still uses an NVARCHAR-family type.', 1;

IF EXISTS (
    SELECT required.TableName
    FROM (VALUES
        (N'AgEvaluationModelJudgement'),
        (N'AgEvaluationModelJudgementEvaluator'),
        (N'AgEvaluationModelJudgementMinimumScore'),
        (N'AgEvaluationModelJudgementCase'),
        (N'AgEvaluationModelJudgementMetric'),
        (N'AgEvaluationModelJudgementDiagnostic')) required(TableName)
    WHERE OBJECT_ID(N'dbo.' + required.TableName, N'U') IS NULL)
    THROW 51742, N'An Evaluation Model Judgement normalized table is missing.', 1;

PRINT N'Evaluation Model Judgement normalized character types verified.';
GO
