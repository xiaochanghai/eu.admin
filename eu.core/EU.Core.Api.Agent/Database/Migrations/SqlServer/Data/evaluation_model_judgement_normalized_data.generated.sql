-- Normalize Evaluation Model Judgements exported from current SQL Server data.
-- Source row-set SHA-256: 23a7acaed4048d9bddcb4fcd3b011fc32963bf816a6ab7164d30a619d23e0e0d
-- Run 035 and 036 first, then this script, then Data/037.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'DocumentJson') IS NULL
    THROW 51711, N'DocumentJson is absent; the cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationModelJudgementNormalizationCheckpoint (JudgementId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);

    -- Evaluation Model Judgement b8027742-c772-427d-aefd-38146caec165
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'10.6.0'))) <> CONVERT(VARBINARY(MAX), N'10.6.0')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8'))) <> CONVERT(VARBINARY(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:29.5347562+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:29.5347562+00:00')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:57.9351001+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:57.9351001+00:00')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec'))) <> CONVERT(VARBINARY(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8b27ac6d-1030-4459-b373-ef1de82b8902'))) <> CONVERT(VARBINARY(MAX), N'8b27ac6d-1030-4459-b373-ef1de82b8902')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'91c97ad482d34d4b83577b8dad4b0715d094787d1f0f01c99ed5ae6ffc3ef93d'))) <> CONVERT(VARBINARY(MAX), N'91c97ad482d34d4b83577b8dad4b0715d094787d1f0f01c99ed5ae6ffc3ef93d')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Coherence'))) <> CONVERT(VARBINARY(MAX), N'Coherence')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Microsoft.Extensions.AI.Evaluation.Quality'))) <> CONVERT(VARBINARY(MAX), N'Microsoft.Extensions.AI.Evaluation.Quality')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Relevance'))) <> CONVERT(VARBINARY(MAX), N'Relevance')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a19e3d49-a763-46d4-802a-d4483fb87944'))) <> CONVERT(VARBINARY(MAX), N'a19e3d49-a763-46d4-802a-d4483fb87944')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b8027742-c772-427d-aefd-38146caec165'))) <> CONVERT(VARBINARY(MAX), N'b8027742-c772-427d-aefd-38146caec165')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'builtin-quality-prompts@10.6.0'))) <> CONVERT(VARBINARY(MAX), N'builtin-quality-prompts@10.6.0')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'))) <> CONVERT(VARBINARY(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5'))) <> CONVERT(VARBINARY(MAX), N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'metric-value-missing'))) <> CONVERT(VARBINARY(MAX), N'metric-value-missing')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'qwen3.7-plus'))) <> CONVERT(VARBINARY(MAX), N'qwen3.7-plus')
        THROW 51713, N'Evaluation Model Judgement text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationModelJudgement SET
        RequestedByUserId = N'development-operator',
        SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'),
        SuiteVersionId = CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'),
        SuiteVersionContentSha256 = N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec',
        Provider = N'Microsoft.Extensions.AI.Evaluation.Quality',
        PackageVersion = N'10.6.0',
        ModelProfileId = N'qwen3.7-plus',
        PromptVersion = N'builtin-quality-prompts@10.6.0',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:29.5347562+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:57.9351001+00:00', 127)),
        AdvisoryPassed = 0
    WHERE ID = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165') AND TenantId = N'development' AND BatchId = CONVERT(uniqueidentifier, N'8b27ac6d-1030-4459-b373-ef1de82b8902') AND ConfigurationSha256 = N'91c97ad482d34d4b83577b8dad4b0715d094787d1f0f01c99ed5ae6ffc3ef93d';
    IF @@ROWCOUNT <> 1 THROW 51712, N'Evaluation Model Judgement source row was not found.', 1;
    DELETE diagnosticRow FROM dbo.AgEvaluationModelJudgementDiagnostic AS diagnosticRow WHERE diagnosticRow.JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165');
    DELETE metricRow FROM dbo.AgEvaluationModelJudgementMetric AS metricRow WHERE metricRow.JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165');
    DELETE caseRow FROM dbo.AgEvaluationModelJudgementCase AS caseRow WHERE caseRow.JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165');
    DELETE scoreRow FROM dbo.AgEvaluationModelJudgementMinimumScore AS scoreRow WHERE scoreRow.JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165');
    DELETE evaluatorRow FROM dbo.AgEvaluationModelJudgementEvaluator AS evaluatorRow WHERE evaluatorRow.JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165');
    INSERT INTO dbo.AgEvaluationModelJudgementEvaluator (ID, JudgementId, Ordinal, Name)
    VALUES (CONVERT(uniqueidentifier, N'a7964c19-b51d-5a87-9170-5f44bf03453f'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), 0, N'Relevance');
    INSERT INTO dbo.AgEvaluationModelJudgementEvaluator (ID, JudgementId, Ordinal, Name)
    VALUES (CONVERT(uniqueidentifier, N'6d2f9105-71c0-5446-93cc-79b7084156a9'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), 1, N'Coherence');
    INSERT INTO dbo.AgEvaluationModelJudgementMinimumScore (ID, JudgementId, Ordinal, Name, Score)
    VALUES (CONVERT(uniqueidentifier, N'608d52bd-d2a7-5f76-85ed-24cbabca52d9'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), 0, N'Relevance', 4);
    INSERT INTO dbo.AgEvaluationModelJudgementMinimumScore (ID, JudgementId, Ordinal, Name, Score)
    VALUES (CONVERT(uniqueidentifier, N'd4343606-9b53-5737-83a7-a55bd4d05bb1'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), 1, N'Coherence', 4);
    INSERT INTO dbo.AgEvaluationModelJudgementCase (ID, JudgementId, Ordinal, CaseId, CaseName, UnifiedRunId, InputSha256, OutputSha256)
    VALUES (CONVERT(uniqueidentifier, N'684a7738-6a7b-57f4-869e-f9bedcc08cf4'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', CONVERT(uniqueidentifier, N'a19e3d49-a763-46d4-802a-d4483fb87944'), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675', N'ec070c2fdd5e6b8ef41c590a6a264430aab01ef34654987d343f7b2cf157d0b5');
    INSERT INTO dbo.AgEvaluationModelJudgementMetric (ID, JudgementId, JudgementCaseId, Ordinal, Name, Score, MinimumScore, Passed)
    VALUES (CONVERT(uniqueidentifier, N'9d4b11d2-b081-5d55-bb1d-0ff303d4a68e'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), CONVERT(uniqueidentifier, N'684a7738-6a7b-57f4-869e-f9bedcc08cf4'), 0, N'Relevance', NULL, 4, 0);
    INSERT INTO dbo.AgEvaluationModelJudgementDiagnostic (ID, JudgementId, JudgementMetricId, Ordinal, Code)
    VALUES (CONVERT(uniqueidentifier, N'1a4ecba2-91ff-5a95-bb0c-d9240f7d75c8'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), CONVERT(uniqueidentifier, N'9d4b11d2-b081-5d55-bb1d-0ff303d4a68e'), 0, N'metric-value-missing');
    INSERT INTO dbo.AgEvaluationModelJudgementMetric (ID, JudgementId, JudgementCaseId, Ordinal, Name, Score, MinimumScore, Passed)
    VALUES (CONVERT(uniqueidentifier, N'5dda8c47-9199-5a25-8627-b44dd6daeaef'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), CONVERT(uniqueidentifier, N'684a7738-6a7b-57f4-869e-f9bedcc08cf4'), 1, N'Coherence', NULL, 4, 0);
    INSERT INTO dbo.AgEvaluationModelJudgementDiagnostic (ID, JudgementId, JudgementMetricId, Ordinal, Code)
    VALUES (CONVERT(uniqueidentifier, N'04a0aea0-f2d6-5b44-a3d4-9cd37b3e3029'), CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'), CONVERT(uniqueidentifier, N'5dda8c47-9199-5a25-8627-b44dd6daeaef'), 0, N'metric-value-missing');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgementNormalizationCheckpoint WHERE JudgementId = CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'))
        INSERT INTO dbo.AgEvaluationModelJudgementNormalizationCheckpoint (JudgementId) VALUES (CONVERT(uniqueidentifier, N'b8027742-c772-427d-aefd-38146caec165'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
