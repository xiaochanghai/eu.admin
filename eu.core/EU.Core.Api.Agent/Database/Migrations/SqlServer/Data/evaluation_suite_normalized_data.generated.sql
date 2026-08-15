-- Normalize Evaluation Suites exported from current SQL Server data.
-- Source row-set SHA-256: b0c3fd564f6f0e0b38ef5256659ecc46ca529562f3d97cf4786747f7f9c708b9
-- Run 025 and 026 first, then this script, then Data/027.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'DocumentJson') IS NULL
    THROW 51511, N'DocumentJson is absent; Evaluation Suite cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgEvaluationSuiteNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgEvaluationSuiteNormalizationCheckpoint (SuiteId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);

    -- Evaluation Suite a2a38ca4-3afb-4e09-a963-1d7045b5bb9b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1.0.0'))) <> CONVERT(VARBINARY(MAX), N'1.0.0')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8'))) <> CONVERT(VARBINARY(MAX), N'197be697-5f19-4271-8d43-22dc5de879f8')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2.0.0'))) <> CONVERT(VARBINARY(MAX), N'2.0.0')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:37.5097069+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:37.5097069+00:00')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:38.2921942+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:38.2921942+00:00')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:11.5468551+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:11.5468551+00:00')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e'))) <> CONVERT(VARBINARY(MAX), N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec'))) <> CONVERT(VARBINARY(MAX), N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'984e9954-e66c-49dd-bbcc-17875546f753'))) <> CONVERT(VARBINARY(MAX), N'984e9954-e66c-49dd-bbcc-17875546f753')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Citation'))) <> CONVERT(VARBINARY(MAX), N'Citation')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'NOT FOUND'))) <> CONVERT(VARBINARY(MAX), N'NOT FOUND')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ORCHID-7319'))) <> CONVERT(VARBINARY(MAX), N'ORCHID-7319')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'One-case PDF grounded model judge acceptance'))) <> CONVERT(VARBINARY(MAX), N'One-case PDF grounded model judge acceptance')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'PDF escalation code'))) <> CONVERT(VARBINARY(MAX), N'PDF escalation code')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Real Model Judge Acceptance'))) <> CONVERT(VARBINARY(MAX), N'Real Model Judge Acceptance')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'What is the Atlas service escalation code? Answer only the exact code.'))) <> CONVERT(VARBINARY(MAX), N'What is the Atlas service escalation code? Answer only the exact code.')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))) <> CONVERT(VARBINARY(MAX), N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'))) <> CONVERT(VARBINARY(MAX), N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development'))) <> CONVERT(VARBINARY(MAX), N'development')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'development-operator'))) <> CONVERT(VARBINARY(MAX), N'development-operator')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f8bddbf5-4fb7-43c9-b721-afb2394a3181'))) <> CONVERT(VARBINARY(MAX), N'f8bddbf5-4fb7-43c9-b721-afb2394a3181')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'message'))) <> CONVERT(VARBINARY(MAX), N'message')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'real-judge-173837'))) <> CONVERT(VARBINARY(MAX), N'real-judge-173837')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'skill-started'))) <> CONVERT(VARBINARY(MAX), N'skill-started')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'tool-succeeded'))) <> CONVERT(VARBINARY(MAX), N'tool-succeeded')
        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgEvaluationSuite SET
        Name = N'Real Model Judge Acceptance',
        Description = N'One-case PDF grounded model judge acceptance',
        Status = N'Active',
        CreatedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:37.5097069+00:00', 127)),
        UpdatedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:11.5468551+00:00', 127)),
        CreatedByUserId = N'development-operator',
        UpdatedByUserId = N'development-operator'
    WHERE ID = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b') AND TenantId = N'development' AND Code = N'real-judge-173837' AND LogicalRevision = 4;
    IF @@ROWCOUNT <> 1 THROW 51512, N'Evaluation Suite source row was not found.', 1;
    DELETE caseRule FROM dbo.AgEvaluationCaseRule AS caseRule WHERE caseRule.SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b');
    DELETE evaluationCase FROM dbo.AgEvaluationCase evaluationCase WHERE evaluationCase.SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b');
    DELETE version FROM dbo.AgEvaluationSuiteVersion version WHERE version.SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b');
    INSERT INTO dbo.AgEvaluationSuiteVersion (ID, SuiteId, Ordinal, Label, IsDraft, ContentSha256, PublishedAtUtc, PublishedByUserId)
    VALUES (CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), 0, N'draft', 1, N'', NULL, N'');
    INSERT INTO dbo.AgEvaluationCase (ID, SuiteId, VersionId, Ordinal, CaseId, Name, Input, TargetAgentId, TargetAgentVersionId, ExpectedStatus, MaximumToolCalls, MaximumDurationMilliseconds)
    VALUES (CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', N'What is the Atlas service escalation code? Answer only the exact code.', CONVERT(uniqueidentifier, N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'), CONVERT(uniqueidentifier, N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'), N'Completed', 0, 120000);
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'6164b799-ebd1-5a46-a3fc-d45a48ac7be0'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), N'OutputContains', 0, N'ORCHID-7319');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'506da30d-ec5d-5e0f-8ca6-099f70339991'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), N'OutputExcludes', 0, N'NOT FOUND');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'b03290fb-71b8-56c5-beac-00831d055123'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), N'RequiredEventKind', 0, N'skill-started');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'38c435bd-60e6-5485-bfe6-49db806edc77'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), N'RequiredEventKind', 1, N'tool-succeeded');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'e949d347-5363-5240-8f2f-f482f72572e9'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'241b8238-27ec-5729-95b4-f433faafbfcc'), CONVERT(uniqueidentifier, N'cf3838a0-7472-599d-9099-9c55fa2e6476'), N'RequiredEventKind', 2, N'message');
    INSERT INTO dbo.AgEvaluationSuiteVersion (ID, SuiteId, Ordinal, Label, IsDraft, ContentSha256, PublishedAtUtc, PublishedByUserId)
    VALUES (CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), 1, N'1.0.0', 0, N'5b2c79267e1cd8e51286d2191d46008d84bb0fe544da1fdb1023cf742d5af15e', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:38.2921942+00:00', 127)), N'development-operator');
    INSERT INTO dbo.AgEvaluationCase (ID, SuiteId, VersionId, Ordinal, CaseId, Name, Input, TargetAgentId, TargetAgentVersionId, ExpectedStatus, MaximumToolCalls, MaximumDurationMilliseconds)
    VALUES (CONVERT(uniqueidentifier, N'448fad44-12b8-5221-b086-2e02319c7d16'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753'), 0, CONVERT(uniqueidentifier, N'f8bddbf5-4fb7-43c9-b721-afb2394a3181'), N'PDF escalation code', N'What is the Atlas service escalation code? Answer only the exact code.', CONVERT(uniqueidentifier, N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'), CONVERT(uniqueidentifier, N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'), N'Completed', 0, 120000);
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'ee2628cb-6eac-5377-9a5b-996634d26751'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753'), CONVERT(uniqueidentifier, N'448fad44-12b8-5221-b086-2e02319c7d16'), N'OutputContains', 0, N'ORCHID-7319');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'5dde631f-2c5b-5d36-9b63-c376b0f7b92d'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753'), CONVERT(uniqueidentifier, N'448fad44-12b8-5221-b086-2e02319c7d16'), N'OutputExcludes', 0, N'NOT FOUND');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'bd89dcd6-ee3c-5977-aa2f-d25a7d5e6c85'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'984e9954-e66c-49dd-bbcc-17875546f753'), CONVERT(uniqueidentifier, N'448fad44-12b8-5221-b086-2e02319c7d16'), N'RequiredEventKind', 0, N'Citation');
    INSERT INTO dbo.AgEvaluationSuiteVersion (ID, SuiteId, Ordinal, Label, IsDraft, ContentSha256, PublishedAtUtc, PublishedByUserId)
    VALUES (CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), 2, N'2.0.0', 0, N'68ad1c2b02bbe041234e9ea1c6e528e3453b958d71d9d0818e2b2342e7fa8dec', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:11.5468551+00:00', 127)), N'development-operator');
    INSERT INTO dbo.AgEvaluationCase (ID, SuiteId, VersionId, Ordinal, CaseId, Name, Input, TargetAgentId, TargetAgentVersionId, ExpectedStatus, MaximumToolCalls, MaximumDurationMilliseconds)
    VALUES (CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), 0, CONVERT(uniqueidentifier, N'c5e67c8d-dde1-46ff-b9fd-3a5bc7b460f7'), N'PDF escalation code', N'What is the Atlas service escalation code? Answer only the exact code.', CONVERT(uniqueidentifier, N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'), CONVERT(uniqueidentifier, N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'), N'Completed', 0, 120000);
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'4c2b597e-0916-5c13-925c-7da23ca6f34e'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), N'OutputContains', 0, N'ORCHID-7319');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'ab7dbb9e-b22a-52fb-9afe-12b54635def2'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), N'OutputExcludes', 0, N'NOT FOUND');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'129cb398-900a-5f70-8400-bc9f0d5dec63'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), N'RequiredEventKind', 0, N'skill-started');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'7b342fc0-14f3-5468-b052-a964931d04ad'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), N'RequiredEventKind', 1, N'tool-succeeded');
    INSERT INTO dbo.AgEvaluationCaseRule (ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)
    VALUES (CONVERT(uniqueidentifier, N'86c8dfad-e22e-52b2-a0ee-b9645ed8d8c5'), CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'), CONVERT(uniqueidentifier, N'197be697-5f19-4271-8d43-22dc5de879f8'), CONVERT(uniqueidentifier, N'c8c1712b-a99b-5311-a6ab-c557453e6ed6'), N'RequiredEventKind', 2, N'message');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationSuiteNormalizationCheckpoint WHERE SuiteId = CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'))
        INSERT INTO dbo.AgEvaluationSuiteNormalizationCheckpoint (SuiteId) VALUES (CONVERT(uniqueidentifier, N'a2a38ca4-3afb-4e09-a963-1d7045b5bb9b'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
