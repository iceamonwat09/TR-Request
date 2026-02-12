-- =====================================================
-- Fix #2: EmailLogs - ALTER SentDate, Status → NOT NULL
-- Purpose: แก้ IX_EmailLogs_SentDate ที่บล็อก ALTER
-- Strategy: DROP ทุก non-PK index ก่อน → ALTER → Recreate
-- Database: HRDSYSTEM
-- Date: 2026-02-12
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '=================================================================='
PRINT '  Fix #2: EmailLogs SentDate & Status → NOT NULL'
PRINT '  Started: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
PRINT ''

-- =====================================================
-- Step 1: สำรวจ Index ทั้งหมด (ก่อนแก้)
-- =====================================================
PRINT '--- Current Indexes on EmailLogs ---'
SELECT
    i.name AS [Index_Name],
    i.type_desc AS [Type],
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
-- Step 2: Drop ทุก non-clustered index (ยกเว้น PK)
-- =====================================================
PRINT ''
PRINT '--- Step 2: Drop ALL non-clustered indexes ---'

DECLARE @sql NVARCHAR(MAX) = ''
DECLARE @idx_name NVARCHAR(200)

DECLARE idx_cursor CURSOR FOR
    SELECT i.name
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID('dbo.EmailLogs')
      AND i.type_desc = 'NONCLUSTERED'
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
    ORDER BY i.name

OPEN idx_cursor
FETCH NEXT FROM idx_cursor INTO @idx_name

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = 'DROP INDEX [' + @idx_name + '] ON dbo.EmailLogs'
    EXEC sp_executesql @sql
    PRINT '   Dropped: ' + @idx_name
    FETCH NEXT FROM idx_cursor INTO @idx_name
END

CLOSE idx_cursor
DEALLOCATE idx_cursor
GO

-- =====================================================
-- Step 3: Drop Default Constraint บน SentDate (ถ้า recreate แล้ว)
-- =====================================================
PRINT ''
PRINT '--- Step 3: Drop Default Constraints ---'

DECLARE @df NVARCHAR(200)

-- SentDate
SELECT @df = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'SentDate'

IF @df IS NOT NULL
BEGIN
    EXEC('ALTER TABLE dbo.EmailLogs DROP CONSTRAINT [' + @df + ']')
    PRINT '   Dropped: ' + @df + ' (SentDate)'
END
ELSE
    PRINT '   SentDate - no default constraint (OK)'
GO

-- =====================================================
-- Step 4: ALTER SentDate, Status → NOT NULL
-- =====================================================
PRINT ''
PRINT '--- Step 4: ALTER columns to NOT NULL ---'

-- Check current state first
SELECT
    c.name,
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS [Current_State]
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.EmailLogs')
AND c.name IN ('SentDate', 'Status')
ORDER BY c.name;

-- SentDate
ALTER TABLE dbo.EmailLogs ALTER COLUMN [SentDate] DATETIME2 NOT NULL
PRINT '   SentDate -> NOT NULL'

-- Status
ALTER TABLE dbo.EmailLogs ALTER COLUMN [Status] NVARCHAR(40) NOT NULL
PRINT '   Status -> NOT NULL'
GO

-- =====================================================
-- Step 5: Recreate Default Constraints
-- =====================================================
PRINT ''
PRINT '--- Step 5: Recreate Default Constraints ---'

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
-- Step 6: Recreate Indexes (เฉพาะที่ Dev มี)
-- =====================================================
PRINT ''
PRINT '--- Step 6: Recreate Indexes (match Dev) ---'

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'IX_EmailLogs_TrainingRequestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailLogs_TrainingRequestId] ON dbo.EmailLogs ([TrainingRequestId])
    PRINT '   Created: IX_EmailLogs_TrainingRequestId'
END
ELSE
    PRINT '   IX_EmailLogs_TrainingRequestId - already exists (OK)'

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'IX_EmailLogs_DocNo')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmailLogs_DocNo] ON dbo.EmailLogs ([DocNo])
    PRINT '   Created: IX_EmailLogs_DocNo'
END
ELSE
    PRINT '   IX_EmailLogs_DocNo - already exists (OK)'

-- ไม่ recreate IX_EmailLogs_RecipientEmail และ IX_EmailLogs_SentDate
-- เพราะ Dev ไม่มี index เหล่านี้
PRINT ''
PRINT '   NOTE: IX_EmailLogs_RecipientEmail - NOT recreated (Dev does not have)'
PRINT '   NOTE: IX_EmailLogs_SentDate - NOT recreated (Dev does not have)'
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  VERIFICATION'
PRINT '=================================================================='

PRINT ''
PRINT '--- EmailLogs All Columns ---'
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
PRINT '--- EmailLogs Indexes (Final) ---'
SELECT
    i.name AS [Index_Name],
    i.type_desc AS [Type],
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

PRINT ''
PRINT '--- Nullable Check (4 target columns) ---'
SELECT
    c.name AS [Column_Name],
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS [Nullable],
    CASE
        WHEN c.is_nullable = 0 THEN 'PASS'
        ELSE 'FAIL - still nullable'
    END AS [Result]
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.EmailLogs')
AND c.name IN ('RecipientEmail', 'EmailType', 'SentDate', 'Status')
ORDER BY c.name;

PRINT ''
PRINT '=================================================================='
PRINT '  Fix #2 Completed!'
PRINT '  Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
GO
