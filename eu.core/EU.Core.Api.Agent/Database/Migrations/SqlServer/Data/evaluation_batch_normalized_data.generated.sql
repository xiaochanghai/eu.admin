-- Normalize Evaluation Batches exported from current SQL Server data.
-- Source row-set SHA-256: 8fcd1a55f43245e2ad9e024d1e240b099516d033039368a994cd26b0da8cf5dc
-- Run 030 and 031 first, then this script, then Data/032.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'DocumentJson') IS NULL
    THROW 51611, N'DocumentJson is absent; Evaluation Batch cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgEvaluationBatchNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);

    -- Evaluation Batch 3c036466-1b1f-495b-9b9e-c92207f8fb0b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0'))) <> CONVERT(VARBINARY(MAX), N'0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:38.6976307+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:38.6976307+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:47.5961032+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:47.5961032+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:47.7148043+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:47.7148043+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'))) <> CONVERT(VARBINARY(MAX), N'3c036466-1b1f-495b-9b9e-c92207f8fb0b')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'469badb9-0f72-48fa-bc96-dd316c55abd5'))) <> CONVERT(VARBINARY(MAX), N'469badb9-0f72-48fa-bc96-dd316c55abd5')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e'))) <> CONVERT(VARBINARY(MAX), N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8050 ms'))) <> CONVERT(VARBINARY(MAX), N'8050 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'984e9954-e66c-49dd-bbcc-17875546f753'))) <> CONVERT(VARBINARY(MAX), N'984e9954-e66c-49dd-bbcc-17875546f753')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 0'))) <> CONVERT(VARBINARY(MAX), N'<= 0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 120000 ms'))) <> CONVERT(VARBINARY(MAX), N'<= 120000 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'EVALUATION_BATCH_ASSERTION_FAILED'))) <> CONVERT(VARBINARY(MAX), N'EVALUATION_BATCH_ASSERTION_FAILED')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'absent'))) <> CONVERT(VARBINARY(MAX), N'absent')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'completed'))) <> CONVERT(VARBINARY(MAX), N'completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'duration'))) <> CONVERT(VARBINARY(MAX), N'duration')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5'))) <> CONVERT(VARBINARY(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'event-kind'))) <> CONVERT(VARBINARY(MAX), N'event-kind')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f8bddbf5-4fb7-43c9-b721-afb2394a3181'))) <> CONVERT(VARBINARY(MAX), N'f8bddbf5-4fb7-43c9-b721-afb2394a3181')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent-started'))) <> CONVERT(VARBINARY(MAX), N'main-agent-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'message'))) <> CONVERT(VARBINARY(MAX), N'message')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-contains'))) <> CONVERT(VARBINARY(MAX), N'output-contains')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-excludes'))) <> CONVERT(VARBINARY(MAX), N'output-excludes')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'present'))) <> CONVERT(VARBINARY(MAX), N'present')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'route-selected'))) <> CONVERT(VARBINARY(MAX), N'route-selected')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'run-started'))) <> CONVERT(VARBINARY(MAX), N'run-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'skill'))) <> CONVERT(VARBINARY(MAX), N'skill')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'skill-started'))) <> CONVERT(VARBINARY(MAX), N'skill-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'status'))) <> CONVERT(VARBINARY(MAX), N'status')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-call-count'))) <> CONVERT(VARBINARY(MAX), N'tool-call-count')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-succeeded'))) <> CONVERT(VARBINARY(MAX), N'tool-succeeded')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationBatch SET
        RequestedByUserId = N'development-operator',
        SuiteVersionContentSha256 = N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:38.6976307+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:47.7148043+00:00', 127)),
        ErrorCode = N''
    WHERE ID = CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b') AND TenantId = N'development' AND SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b') AND SuiteVersionId = CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753') AND LogicalRevision = 3;
    IF @@ROWCOUNT <> 1 THROW 51612, N'Evaluation Batch source row was not found.', 1;
    DELETE batchCheck FROM dbo.AgEvaluationBatchCheck AS batchCheck WHERE batchCheck.BatchId = CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b');
    DELETE batchObservation FROM dbo.AgEvaluationBatchObservation AS batchObservation WHERE batchObservation.BatchId = CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b');
    DELETE batchCase FROM dbo.AgEvaluationBatchCase AS batchCase WHERE batchCase.BatchId = CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b');
    INSERT INTO dbo.AgEvaluationBatchCase (ID, BatchId, Ordinal, CaseId, CaseName, TargetAgentId, TargetAgentVersionId, Status, UnifiedRunId, UnifiedRunStatus, ErrorCode, DurationMilliseconds, ToolCallCount, ReportEvaluatedAtUtc, ReportPassed, ReportScore, OutputSha256, OutputUtf8Bytes)
    VALUES (CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), 0, CONVERT(uniqueidentifier, N'f8bddbf5-4fb7-43c9-b721-afb2394a3181'), N'PDF escalation code', CONVERT(uniqueidentifier, N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'), CONVERT(uniqueidentifier, N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'), N'Failed', CONVERT(uniqueidentifier, N'469badb9-0f72-48fa-bc96-dd316c55abd5'), N'Completed', N'EVALUATION_BATCH_ASSERTION_FAILED', 8050, 0, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:47.5961032+00:00', 127)), 0, 0.8333, N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5', 11);
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'c07b1821-41da-537b-a302-e43e5dcdb83d'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 0, N'status', 1, N'Completed', N'Completed');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'9ad60c53-b86c-5ae6-b347-8026eb7fc3f1'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 1, N'output-contains', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'680b8fb6-46a5-5b76-815a-be2365869609'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 2, N'output-excludes', 1, N'absent', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'00bcfb13-35d1-51d1-ad28-685331659640'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 3, N'event-kind', 0, N'present', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'e335bb62-c0ab-5ab5-8163-e1eeb4e0676f'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 4, N'tool-call-count', 1, N'<= 0', N'0');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'4cf87661-7e40-5215-b9bb-b54b55f765ef'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), 5, N'duration', 1, N'<= 120000 ms', N'8050 ms');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'4bb3584f-47ca-58a6-a028-841848691cf3'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 0, N'run-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'afa61ab2-2c00-5601-9a00-2772a8b2e360'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 1, N'main-agent-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'e0bcd5d3-ecef-58c4-940f-4ef660b1fb24'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 2, N'route-selected');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'a37182da-0cc4-5f14-a836-9ddfca5c8580'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 3, N'skill-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'be105600-5e9f-5a3b-9934-0af10240790a'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 4, N'tool-succeeded');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'b58da39c-f0c6-593e-9016-7b4697066fbe'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 5, N'message');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'b44dc927-58ae-581f-9153-f4df09eb7cab'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'EventKind', 6, N'completed');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'c6873e92-42d8-5ef9-940d-9debcf60f86c'), CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'), CONVERT(uniqueidentifier, N'4284002a-9f1c-52d4-970a-b606d613a5a6'), N'Route', 0, N'skill');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint WHERE BatchId = CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'))
        INSERT INTO dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId) VALUES (CONVERT(uniqueidentifier, N'3c036466-1b1f-495b-9b9e-c92207f8fb0b'));

    -- Evaluation Batch 8b27ac6d-1030-4459-b373-ef1de82b8902
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0'))) <> CONVERT(VARBINARY(MAX), N'0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8'))) <> CONVERT(VARBINARY(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:11.8726564+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:11.8726564+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:17.9414734+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:17.9414734+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:18.0716127+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:18.0716127+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5461 ms'))) <> CONVERT(VARBINARY(MAX), N'5461 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec'))) <> CONVERT(VARBINARY(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8b27ac6d-1030-4459-b373-ef1de82b8902'))) <> CONVERT(VARBINARY(MAX), N'8b27ac6d-1030-4459-b373-ef1de82b8902')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 0'))) <> CONVERT(VARBINARY(MAX), N'<= 0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 120000 ms'))) <> CONVERT(VARBINARY(MAX), N'<= 120000 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a19e3d49-a763-46d4-802a-d4483fb87944'))) <> CONVERT(VARBINARY(MAX), N'a19e3d49-a763-46d4-802a-d4483fb87944')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'absent'))) <> CONVERT(VARBINARY(MAX), N'absent')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'))) <> CONVERT(VARBINARY(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'completed'))) <> CONVERT(VARBINARY(MAX), N'completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'duration'))) <> CONVERT(VARBINARY(MAX), N'duration')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5'))) <> CONVERT(VARBINARY(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'event-kind'))) <> CONVERT(VARBINARY(MAX), N'event-kind')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent-started'))) <> CONVERT(VARBINARY(MAX), N'main-agent-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'message'))) <> CONVERT(VARBINARY(MAX), N'message')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-contains'))) <> CONVERT(VARBINARY(MAX), N'output-contains')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-excludes'))) <> CONVERT(VARBINARY(MAX), N'output-excludes')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'present'))) <> CONVERT(VARBINARY(MAX), N'present')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'route-selected'))) <> CONVERT(VARBINARY(MAX), N'route-selected')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'run-started'))) <> CONVERT(VARBINARY(MAX), N'run-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'skill'))) <> CONVERT(VARBINARY(MAX), N'skill')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'skill-started'))) <> CONVERT(VARBINARY(MAX), N'skill-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'status'))) <> CONVERT(VARBINARY(MAX), N'status')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-call-count'))) <> CONVERT(VARBINARY(MAX), N'tool-call-count')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-succeeded'))) <> CONVERT(VARBINARY(MAX), N'tool-succeeded')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationBatch SET
        RequestedByUserId = N'development-operator',
        SuiteVersionContentSha256 = N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:11.8726564+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:18.0716127+00:00', 127)),
        ErrorCode = N''
    WHERE ID = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902') AND TenantId = N'development' AND SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b') AND SuiteVersionId = CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8') AND LogicalRevision = 3;
    IF @@ROWCOUNT <> 1 THROW 51612, N'Evaluation Batch source row was not found.', 1;
    DELETE batchCheck FROM dbo.AgEvaluationBatchCheck AS batchCheck WHERE batchCheck.BatchId = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902');
    DELETE batchObservation FROM dbo.AgEvaluationBatchObservation AS batchObservation WHERE batchObservation.BatchId = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902');
    DELETE batchCase FROM dbo.AgEvaluationBatchCase AS batchCase WHERE batchCase.BatchId = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902');
    INSERT INTO dbo.AgEvaluationBatchCase (ID, BatchId, Ordinal, CaseId, CaseName, TargetAgentId, TargetAgentVersionId, Status, UnifiedRunId, UnifiedRunStatus, ErrorCode, DurationMilliseconds, ToolCallCount, ReportEvaluatedAtUtc, ReportPassed, ReportScore, OutputSha256, OutputUtf8Bytes)
    VALUES (CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', CONVERT(uniqueidentifier, N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'), CONVERT(uniqueidentifier, N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'), N'Passed', CONVERT(uniqueidentifier, N'a19e3d49-a763-46d4-802a-d4483fb87944'), N'Completed', N'', 5461, 0, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:17.9414734+00:00', 127)), 1, 1, N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5', 11);
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'9b8ee2e8-b633-5fd4-b60b-7ebc39dc1ab0'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 0, N'status', 1, N'Completed', N'Completed');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'c3936272-275f-5aef-928e-fa1d8c95436d'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 1, N'output-contains', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'd7bb504b-c3d7-596f-827c-74904669d14e'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 2, N'output-excludes', 1, N'absent', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'455a77fd-dd6a-5625-a1b4-b184e648ff2c'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 3, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'cd1f11b6-e7c2-514d-b346-32f4ca4ca399'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 4, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'c1847449-aa15-5bea-8a31-001a8cef438e'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 5, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'2073146f-f65a-5810-a8e0-8315bd868e62'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 6, N'tool-call-count', 1, N'<= 0', N'0');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'de4840a2-8e2f-56d8-8a61-ae79d8188eb7'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), 7, N'duration', 1, N'<= 120000 ms', N'5461 ms');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'32f715bb-fa84-5811-99c1-a1a9c2106eb8'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 0, N'run-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'6c9b2700-17a5-5132-9f0e-fb362d451ae1'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 1, N'main-agent-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'8f29141e-f13b-5c34-94b7-a2eb666a7ad2'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 2, N'route-selected');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'7c6bd36b-1efa-5953-9bfe-a16607484a6a'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 3, N'skill-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'7144f3bd-348d-5190-96f0-b2f9382d84a0'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 4, N'tool-succeeded');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'7b3a28c5-7df9-514f-9901-10213ff840ad'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 5, N'message');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'2834643a-9799-5760-b474-2b3fb5022611'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'EventKind', 6, N'completed');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'24dd4fd2-71b3-5159-9b75-accb868ebe37'), CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'), CONVERT(uniqueidentifier, N'fe37db36-7b13-5443-9b9b-12a5a38946e9'), N'Route', 0, N'skill');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint WHERE BatchId = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'))
        INSERT INTO dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId) VALUES (CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902'));

    -- Evaluation Batch 7244424b-29c0-4efe-a566-2425ca556d9a
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0'))) <> CONVERT(VARBINARY(MAX), N'0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:39:30.4825228+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:39:30.4825228+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:39:35.7235857+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:39:35.7235857+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:39:35.8837222+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:39:35.8837222+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'395b69529953e959224319676ab4da794bde1c97162fbee024ca4c18d561786f'))) <> CONVERT(VARBINARY(MAX), N'395b69529953e959224319676ab4da794bde1c97162fbee024ca4c18d561786f')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4440 ms'))) <> CONVERT(VARBINARY(MAX), N'4440 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4ecdae9b-58d4-4a36-95cf-d67e04fc76f8'))) <> CONVERT(VARBINARY(MAX), N'4ecdae9b-58d4-4a36-95cf-d67e04fc76f8')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'7244424b-29c0-4efe-a566-2425ca556d9a'))) <> CONVERT(VARBINARY(MAX), N'7244424b-29c0-4efe-a566-2425ca556d9a')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 0'))) <> CONVERT(VARBINARY(MAX), N'<= 0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 120000 ms'))) <> CONVERT(VARBINARY(MAX), N'<= 120000 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'EVALUATION_BATCH_ASSERTION_FAILED'))) <> CONVERT(VARBINARY(MAX), N'EVALUATION_BATCH_ASSERTION_FAILED')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'absent'))) <> CONVERT(VARBINARY(MAX), N'absent')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b3efad8f-fffd-46bd-a3f4-c0d924fd1195'))) <> CONVERT(VARBINARY(MAX), N'b3efad8f-fffd-46bd-a3f4-c0d924fd1195')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'))) <> CONVERT(VARBINARY(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cc00575852073a9390b7682a339ee3c8af06d7e89197cdfa96eb45accc40838f'))) <> CONVERT(VARBINARY(MAX), N'cc00575852073a9390b7682a339ee3c8af06d7e89197cdfa96eb45accc40838f')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'completed'))) <> CONVERT(VARBINARY(MAX), N'completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'direct'))) <> CONVERT(VARBINARY(MAX), N'direct')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'duration'))) <> CONVERT(VARBINARY(MAX), N'duration')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'event-kind'))) <> CONVERT(VARBINARY(MAX), N'event-kind')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'knowledge-citation'))) <> CONVERT(VARBINARY(MAX), N'knowledge-citation')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'knowledge-retrieved'))) <> CONVERT(VARBINARY(MAX), N'knowledge-retrieved')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent-started'))) <> CONVERT(VARBINARY(MAX), N'main-agent-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'message'))) <> CONVERT(VARBINARY(MAX), N'message')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-contains'))) <> CONVERT(VARBINARY(MAX), N'output-contains')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-excludes'))) <> CONVERT(VARBINARY(MAX), N'output-excludes')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'present'))) <> CONVERT(VARBINARY(MAX), N'present')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'route-selected'))) <> CONVERT(VARBINARY(MAX), N'route-selected')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'run-started'))) <> CONVERT(VARBINARY(MAX), N'run-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'status'))) <> CONVERT(VARBINARY(MAX), N'status')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-call-count'))) <> CONVERT(VARBINARY(MAX), N'tool-call-count')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationBatch SET
        RequestedByUserId = N'development-operator',
        SuiteVersionContentSha256 = N'395b69529953e959224319676ab4da794bde1c97162fbee024ca4c18d561786f',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:39:30.4825228+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:39:35.8837222+00:00', 127)),
        ErrorCode = N''
    WHERE ID = CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a') AND TenantId = N'development' AND SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b') AND SuiteVersionId = CONVERT(uniqueidentifier, N'4ecdae9b-58d4-4a36-95cf-d67e04fc76f8') AND LogicalRevision = 3;
    IF @@ROWCOUNT <> 1 THROW 51612, N'Evaluation Batch source row was not found.', 1;
    DELETE batchCheck FROM dbo.AgEvaluationBatchCheck AS batchCheck WHERE batchCheck.BatchId = CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a');
    DELETE batchObservation FROM dbo.AgEvaluationBatchObservation AS batchObservation WHERE batchObservation.BatchId = CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a');
    DELETE batchCase FROM dbo.AgEvaluationBatchCase AS batchCase WHERE batchCase.BatchId = CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a');
    INSERT INTO dbo.AgEvaluationBatchCase (ID, BatchId, Ordinal, CaseId, CaseName, TargetAgentId, TargetAgentVersionId, Status, UnifiedRunId, UnifiedRunStatus, ErrorCode, DurationMilliseconds, ToolCallCount, ReportEvaluatedAtUtc, ReportPassed, ReportScore, OutputSha256, OutputUtf8Bytes)
    VALUES (CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', CONVERT(uniqueidentifier, N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'), CONVERT(uniqueidentifier, N'95dfbfef-4fd0-4c93-8785-6c93035c3574'), N'Failed', CONVERT(uniqueidentifier, N'b3efad8f-fffd-46bd-a3f4-c0d924fd1195'), N'Completed', N'EVALUATION_BATCH_ASSERTION_FAILED', 4440, 0, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:39:35.7235857+00:00', 127)), 0, 0.75, N'cc00575852073a9390b7682a339ee3c8af06d7e89197cdfa96eb45accc40838f', 70);
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'd5a20d05-29ee-5814-aa87-b5c67d83b459'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 0, N'status', 1, N'Completed', N'Completed');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'a410ea27-5897-5a52-ab58-72220fc63b57'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 1, N'output-contains', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'dab36e8c-ebe8-5f21-b957-1b9f319feb36'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 2, N'output-excludes', 1, N'absent', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'21029209-f564-572d-a1e9-bfb3331fb743'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 3, N'event-kind', 0, N'present', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'5ec42f85-db2f-5dda-89bc-041a54526b43'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 4, N'event-kind', 0, N'present', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'de68fdce-f416-5042-aaee-827d3ad27206'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 5, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'dc044dfa-1bf7-5ca5-a3a5-2e21d7b159c2'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 6, N'tool-call-count', 1, N'<= 0', N'0');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'6a9e641d-6c48-5ccb-87b3-dc5b25dac5e5'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), 7, N'duration', 1, N'<= 120000 ms', N'4440 ms');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'b39d3061-d9b9-5083-bec1-a5a5f483d986'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 0, N'run-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'ed304da9-eec4-56d4-b503-77744771a684'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 1, N'main-agent-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'7003d5b2-fa8e-5148-8264-7d7fc5bdab8b'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 2, N'knowledge-retrieved');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'87c2e737-c5e9-55e3-8456-007cdac905e8'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 3, N'knowledge-citation');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'80547d71-c04a-531e-a7dd-c89e35fc4fbb'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 4, N'route-selected');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'9a200462-9af5-5e24-a7ed-f506d2634916'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 5, N'message');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'8a6295b7-208a-52d1-aafe-9fb13718866b'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'EventKind', 6, N'completed');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'a7f1c401-e62c-51b0-ad62-7726cc1149fc'), CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'), CONVERT(uniqueidentifier, N'8b3efe54-0004-5928-8c41-56669e43e983'), N'Route', 0, N'direct');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint WHERE BatchId = CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'))
        INSERT INTO dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId) VALUES (CONVERT(uniqueidentifier, N'7244424b-29c0-4efe-a566-2425ca556d9a'));

    -- Evaluation Batch ee1af2e0-86c1-4f3a-b914-6bbc1200d66e
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0'))) <> CONVERT(VARBINARY(MAX), N'0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:43:16.490331+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:43:16.490331+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:43:20.2739443+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:43:20.2739443+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:43:20.4326083+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:43:20.4326083+00:00')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3069 ms'))) <> CONVERT(VARBINARY(MAX), N'3069 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5b3cafda-b944-43e2-bc73-0b644148b693'))) <> CONVERT(VARBINARY(MAX), N'5b3cafda-b944-43e2-bc73-0b644148b693')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'60b8de81765adfa3a4b2681a5fdfe794708ab07b0bcbaf4be4ccf3d6715c07a5'))) <> CONVERT(VARBINARY(MAX), N'60b8de81765adfa3a4b2681a5fdfe794708ab07b0bcbaf4be4ccf3d6715c07a5')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 0'))) <> CONVERT(VARBINARY(MAX), N'<= 0')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'<= 120000 ms'))) <> CONVERT(VARBINARY(MAX), N'<= 120000 ms')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'absent'))) <> CONVERT(VARBINARY(MAX), N'absent')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'))) <> CONVERT(VARBINARY(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'completed'))) <> CONVERT(VARBINARY(MAX), N'completed')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'direct'))) <> CONVERT(VARBINARY(MAX), N'direct')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'duration'))) <> CONVERT(VARBINARY(MAX), N'duration')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5'))) <> CONVERT(VARBINARY(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'))) <> CONVERT(VARBINARY(MAX), N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'event-kind'))) <> CONVERT(VARBINARY(MAX), N'event-kind')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'fb1eb497-bea9-43a2-b3c8-cb0bd33d8e32'))) <> CONVERT(VARBINARY(MAX), N'fb1eb497-bea9-43a2-b3c8-cb0bd33d8e32')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'knowledge-citation'))) <> CONVERT(VARBINARY(MAX), N'knowledge-citation')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'knowledge-retrieved'))) <> CONVERT(VARBINARY(MAX), N'knowledge-retrieved')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent-started'))) <> CONVERT(VARBINARY(MAX), N'main-agent-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'message'))) <> CONVERT(VARBINARY(MAX), N'message')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-contains'))) <> CONVERT(VARBINARY(MAX), N'output-contains')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'output-excludes'))) <> CONVERT(VARBINARY(MAX), N'output-excludes')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'present'))) <> CONVERT(VARBINARY(MAX), N'present')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'route-selected'))) <> CONVERT(VARBINARY(MAX), N'route-selected')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'run-started'))) <> CONVERT(VARBINARY(MAX), N'run-started')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'status'))) <> CONVERT(VARBINARY(MAX), N'status')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-call-count'))) <> CONVERT(VARBINARY(MAX), N'tool-call-count')
        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationBatch SET
        RequestedByUserId = N'development-operator',
        SuiteVersionContentSha256 = N'60b8de81765adfa3a4b2681a5fdfe794708ab07b0bcbaf4be4ccf3d6715c07a5',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:43:16.490331+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:43:20.4326083+00:00', 127)),
        ErrorCode = N''
    WHERE ID = CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e') AND TenantId = N'development' AND SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b') AND SuiteVersionId = CONVERT(uniqueidentifier, N'fb1eb497-bea9-43a2-b3c8-cb0bd33d8e32') AND LogicalRevision = 3;
    IF @@ROWCOUNT <> 1 THROW 51612, N'Evaluation Batch source row was not found.', 1;
    DELETE batchCheck FROM dbo.AgEvaluationBatchCheck AS batchCheck WHERE batchCheck.BatchId = CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e');
    DELETE batchObservation FROM dbo.AgEvaluationBatchObservation AS batchObservation WHERE batchObservation.BatchId = CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e');
    DELETE batchCase FROM dbo.AgEvaluationBatchCase AS batchCase WHERE batchCase.BatchId = CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e');
    INSERT INTO dbo.AgEvaluationBatchCase (ID, BatchId, Ordinal, CaseId, CaseName, TargetAgentId, TargetAgentVersionId, Status, UnifiedRunId, UnifiedRunStatus, ErrorCode, DurationMilliseconds, ToolCallCount, ReportEvaluatedAtUtc, ReportPassed, ReportScore, OutputSha256, OutputUtf8Bytes)
    VALUES (CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', CONVERT(uniqueidentifier, N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'), CONVERT(uniqueidentifier, N'95dfbfef-4fd0-4c93-8785-6c93035c3574'), N'Passed', CONVERT(uniqueidentifier, N'5b3cafda-b944-43e2-bc73-0b644148b693'), N'Completed', N'', 3069, 0, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:43:20.2739443+00:00', 127)), 1, 1, N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5', 11);
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'32a85146-a7d0-5ab4-865b-b5f9d02b0f19'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 0, N'status', 1, N'Completed', N'Completed');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'334652a5-6efd-51fe-ab42-75f508098708'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 1, N'output-contains', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'4ae3e975-cfef-5ef9-a0e5-4fcb579f0dad'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 2, N'output-excludes', 1, N'absent', N'absent');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'ea4476f0-4e0c-5318-a52b-520827923963'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 3, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'aee69929-01d9-5c9c-8f6d-668890cf8ec1'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 4, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'6a54c899-c417-569e-a5d6-80b584665e1d'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 5, N'event-kind', 1, N'present', N'present');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'981a6556-f9e8-522a-b93a-f015e9b6a0bd'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 6, N'tool-call-count', 1, N'<= 0', N'0');
    INSERT INTO dbo.AgEvaluationBatchCheck (ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)
    VALUES (CONVERT(uniqueidentifier, N'789e0275-6281-5a7a-bc35-89b154236a93'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), 7, N'duration', 1, N'<= 120000 ms', N'3069 ms');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'e76eceae-415c-5ebd-9060-7c94308350a3'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 0, N'run-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'fde8eaee-650a-53b5-8ebe-b1a6cfae5de2'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 1, N'main-agent-started');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'fb24808b-9b09-5963-a621-e8b7ffe34f0f'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 2, N'knowledge-retrieved');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'65972018-e74b-585c-9661-0df4149675a3'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 3, N'knowledge-citation');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'8f04f8d6-fdb8-5cd7-9430-98256617638e'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 4, N'route-selected');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'e19875a6-9fea-5e6e-9f94-cc9447a72581'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 5, N'message');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'90607769-3453-51db-9322-6f52db69a39f'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'EventKind', 6, N'completed');
    INSERT INTO dbo.AgEvaluationBatchObservation (ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'a2d025fa-29b1-5e01-adea-684ddaeac634'), CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'), CONVERT(uniqueidentifier, N'a034e735-82a1-57a8-a9e4-3b849bef9e8e'), N'Route', 0, N'direct');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint WHERE BatchId = CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'))
        INSERT INTO dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId) VALUES (CONVERT(uniqueidentifier, N'ee1af2e0-86c1-4f3a-b914-6bbc1200d66e'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
