SET NOCOUNT ON;

-- Retaining this guard keeps 0001 safe to apply in the documented order and
-- in a partially prepared database.
IF SCHEMA_ID(N'agent') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA [agent]');
END;
