-- =====================================================
-- Fix: EmailLogs - ALTER 4 columns → NOT NULL
-- Purpose: แก้ไขที่ Migration ทำไม่สำเร็จ เพราะมี Index ค้าง
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-12
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '=================================================================='
PRINT '  Fix EmailLogs Nullable (Drop Index → ALTER → Recreate Index)'
PRINT '  Started: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
PRINT ''

-- =====================================================
-- Step 1: สำรวจ Index ทั้งหมดบน EmailLogs (ยกเว้น PK)
-- =====================================================
PRINT '--- Current Indexes on EmailLogs ---'
SELECT
    i.name AS [Index_Name],
    i.type_desc AS [Type],
    CASE WHEN i.is_unique = 1 THEN 'YES' ELSE 'NO' END AS [Is_Unique],
    STUFF((
        SELECT ', ' + c2.name
        FROM sys.index_columns ic2
        INNER JOIN sys.columns c2 ON ic2.object_id = c2.object_id AND ic2.column_id = c2.column_id
        WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id
        ORDER BY ic2.key_ordinal
        FOR XML PATH(''), TYPE
    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS [Columns]
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.EmailLogs')
AND i.index_id > 0
ORDER BY i.name;
GO

-- =====================================================
-- Step 2: Drop Index ที่ติด RecipientEmail
-- =====================================================
PRINT ''
PRINT '--- Step 2: Drop Indexes ที่กระทบ ---'

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'IX_EmailLogs_RecipientEmail')
BEGIN
    DROP INDEX [IX_EmailLogs_RecipientEmail] ON dbo.EmailLogs
    PRINT '   Dropped: IX_EmailLogs_RecipientEmail'
END
ELSE
    PRINT '   IX_EmailLogs_RecipientEmail - not found (OK)'
GO

-- =====================================================
-- Step 3: Update NULL data ก่อน ALTER
-- =====================================================
PRINT ''
PRINT '--- Step 3: Update NULL data ---'

DECLARE @cnt INT

UPDATE dbo.EmailLogs SET RecipientEmail = '' WHERE RecipientEmail IS NULL
SET @cnt = @@ROWCOUNT
PRINT '   RecipientEmail: updated ' + CAST(@cnt AS VARCHAR) + ' NULL rows'

UPDATE dbo.EmailLogs SET EmailType = '' WHERE EmailType IS NULL
SET @cnt = @@ROWCOUNT
PRINT '   EmailType: updated ' + CAST(@cnt AS VARCHAR) + ' NULL rows'

UPDATE dbo.EmailLogs SET SentDate = GETDATE() WHERE SentDate IS NULL
SET @cnt = @@ROWCOUNT
PRINT '   SentDate: updated ' + CAST(@cnt AS VARCHAR) + ' NULL rows'

UPDATE dbo.EmailLogs SET Status = 'Unknown' WHERE Status IS NULL
SET @cnt = @@ROWCOUNT
PRINT '   Status: updated ' + CAST(@cnt AS VARCHAR) + ' NULL rows'
GO

-- =====================================================
-- Step 4: Drop Default Constraints ก่อน ALTER
-- =====================================================
PRINT ''
PRINT '--- Step 4: Drop Default Constraints ---'

-- SentDate default
DECLARE @df_sent NVARCHAR(200)
SELECT @df_sent = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'SentDate'

IF @df_sent IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.EmailLogs DROP CONSTRAINT [' + @df_sent + ']')
    PRINT '   Dropped: ' + @df_sent + ' (SentDate)'
END

-- RetryCount default
DECLARE @df_retry NVARCHAR(200)
SELECT @df_retry = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'RetryCount'

IF @df_retry IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.EmailLogs DROP CONSTRAINT [' + @df_retry + ']')
    PRINT '   Dropped: ' + @df_retry + ' (RetryCount)'
END
GO

-- =====================================================
-- Step 5: ALTER columns → NOT NULL
-- =====================================================
PRINT ''
PRINT '--- Step 5: ALTER columns to NOT NULL ---'

ALTER TABLE dbo.EmailLogs ALTER COLUMN [RecipientEmail] NVARCHAR(200) NOT NULL
PRINT '   RecipientEmail → NOT NULL'

ALTER TABLE dbo.EmailLogs ALTER COLUMN [EmailType] NVARCHAR(100) NOT NULL
PRINT '   EmailType → NOT NULL'

ALTER TABLE dbo.EmailLogs ALTER COLUMN [SentDate] DATETIME2 NOT NULL
PRINT '   SentDate → NOT NULL'

ALTER TABLE dbo.EmailLogs ALTER COLUMN [Status] NVARCHAR(40) NOT NULL
PRINT '   Status → NOT NULL'
GO

-- =====================================================
-- Step 6: Recreate Default Constraints
-- =====================================================
PRINT ''
PRINT '--- Step 6: Recreate Default Constraints ---'

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'SentDate'
)
BEGIN
    ALTER TABLE dbo.EmailLogs ADD CONSTRAINT DF_EmailLogs_SentDate DEFAULT (GETDATE()) FOR [SentDate]
    PRINT '   Created: DF_EmailLogs_SentDate (GETDATE())'
END

IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'RetryCount'
)
BEGIN
    ALTER TABLE dbo.EmailLogs ADD CONSTRAINT DF_EmailLogs_RetryCount DEFAULT ((0)) FOR [RetryCount]
    PRINT '   Created: DF_EmailLogs_RetryCount (0)'
END
GO

-- =====================================================
-- Step 7: Recreate Indexes (ตาม Dev)
-- =====================================================
PRINT ''
PRINT '--- Step 7: Recreate Indexes ---'

-- IX_EmailLogs_TrainingRequestId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'IX_EmailLogs_TrainingRequestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailLogs_TrainingRequestId] ON dbo.EmailLogs ([TrainingRequestId])
    PRINT '   Created: IX_EmailLogs_TrainingRequestId'
END
ELSE
    PRINT '   IX_EmailLogs_TrainingRequestId - already exists (OK)'

-- IX_EmailLogs_DocNo
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'IX_EmailLogs_DocNo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailLogs_DocNo] ON dbo.EmailLogs ([DocNo])
    PRINT '   Created: IX_EmailLogs_DocNo'
END
ELSE
    PRINT '   IX_EmailLogs_DocNo - already exists (OK)'
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  VERIFICATION'
PRINT '=================================================================='
PRINT ''

PRINT '--- EmailLogs Columns ---'
SELECT
    c.name AS [Column_Name],
    ty.name AS [Data_Type],
    CASE
        WHEN ty.name IN ('nvarchar','nchar') THEN CAST(c.max_length / 2 AS VARCHAR)
        ELSE CAST(c.max_length AS VARCHAR)
    END AS [Max_Length],
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS [Nullable],
    ISNULL(dc.definition, '') AS [Default]
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
WHERE c.object_id = OBJECT_ID('dbo.EmailLogs')
ORDER BY c.column_id;

PRINT ''
PRINT '--- EmailLogs Indexes ---'
SELECT
    i.name AS [Index_Name],
    i.type_desc AS [Type]
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.EmailLogs')
AND i.index_id > 0
ORDER BY i.name;

PRINT ''
PRINT '=================================================================='
PRINT '  Fix Completed!'
PRINT '  Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
GO
