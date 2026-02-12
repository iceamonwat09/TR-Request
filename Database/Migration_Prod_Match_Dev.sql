-- =====================================================
-- Migration Script: Prod ให้ตรงกับ Dev (Dev เป็นหลัก)
-- Purpose: แก้ไขโครงสร้าง Prod (เครื่องใหม่) ให้ตรงกับ Dev
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-12
-- =====================================================
-- สิ่งที่แก้ไข:
--   1. ApprovalHistory   → DROP 2 columns (DocNo, IpAddress)
--   2. EmailLogs         → ALTER 4 columns เป็น NOT NULL
--   3. RetryEmailHistory → ALTER 4 columns เป็น NULL
--   4. TrainingParticipants → DROP TABLE (Dev ไม่มี)
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '=================================================================='
PRINT '  Migration: Prod → Match Dev'
PRINT '  Started: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
PRINT ''

-- =====================================================
-- 1. ApprovalHistory: DROP DocNo, IpAddress (Dev ไม่มี)
-- =====================================================
PRINT '1. ApprovalHistory - Drop extra columns...'

-- Drop DocNo
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ApprovalHistory')
    AND name = 'DocNo'
)
BEGIN
    -- Drop Default Constraint ก่อน (ถ้ามี)
    DECLARE @df_ah_docno NVARCHAR(200)
    SELECT @df_ah_docno = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'DocNo'

    IF @df_ah_docno IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.ApprovalHistory DROP CONSTRAINT [' + @df_ah_docno + ']')
        PRINT '   Dropped default constraint: ' + @df_ah_docno
    END

    ALTER TABLE dbo.ApprovalHistory DROP COLUMN [DocNo]
    PRINT '   Dropped column: DocNo'
END
ELSE
    PRINT '   DocNo - already not exists (OK)'
GO

-- Drop IpAddress
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ApprovalHistory')
    AND name = 'IpAddress'
)
BEGIN
    DECLARE @df_ah_ip NVARCHAR(200)
    SELECT @df_ah_ip = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'IpAddress'

    IF @df_ah_ip IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.ApprovalHistory DROP CONSTRAINT [' + @df_ah_ip + ']')
        PRINT '   Dropped default constraint: ' + @df_ah_ip
    END

    ALTER TABLE dbo.ApprovalHistory DROP COLUMN [IpAddress]
    PRINT '   Dropped column: IpAddress'
END
ELSE
    PRINT '   IpAddress - already not exists (OK)'
GO

PRINT '   ApprovalHistory - Done (Expected: 9 columns)'
PRINT ''

-- =====================================================
-- 2. EmailLogs: ALTER 4 columns → NOT NULL (ให้ตรง Dev)
-- =====================================================
PRINT '2. EmailLogs - Change nullable to NOT NULL...'

-- 2.1 RecipientEmail: NULL → NOT NULL
-- ต้อง update NULL data ก่อน (ถ้ามี)
UPDATE dbo.EmailLogs SET RecipientEmail = '' WHERE RecipientEmail IS NULL
ALTER TABLE dbo.EmailLogs ALTER COLUMN [RecipientEmail] NVARCHAR(200) NOT NULL
PRINT '   RecipientEmail → NOT NULL'

-- 2.2 EmailType: NULL → NOT NULL
UPDATE dbo.EmailLogs SET EmailType = '' WHERE EmailType IS NULL
ALTER TABLE dbo.EmailLogs ALTER COLUMN [EmailType] NVARCHAR(100) NOT NULL
PRINT '   EmailType → NOT NULL'

-- 2.3 SentDate: NULL → NOT NULL
-- ต้อง update NULL data ก่อน แล้ว re-create default constraint
UPDATE dbo.EmailLogs SET SentDate = GETDATE() WHERE SentDate IS NULL

-- Drop existing default constraint ก่อน ALTER
DECLARE @df_el_sent NVARCHAR(200)
SELECT @df_el_sent = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.EmailLogs') AND c.name = 'SentDate'

IF @df_el_sent IS NOT NULL
    EXEC('ALTER TABLE dbo.EmailLogs DROP CONSTRAINT [' + @df_el_sent + ']')

ALTER TABLE dbo.EmailLogs ALTER COLUMN [SentDate] DATETIME2 NOT NULL

-- Re-create default constraint
ALTER TABLE dbo.EmailLogs ADD CONSTRAINT DF_EmailLogs_SentDate DEFAULT (GETDATE()) FOR [SentDate]
PRINT '   SentDate → NOT NULL (with default GETDATE())'

-- 2.4 Status: NULL → NOT NULL
UPDATE dbo.EmailLogs SET Status = 'Unknown' WHERE Status IS NULL
ALTER TABLE dbo.EmailLogs ALTER COLUMN [Status] NVARCHAR(40) NOT NULL
PRINT '   Status → NOT NULL'
GO

PRINT '   EmailLogs - Done (10 columns, nullable fixed)'
PRINT ''

-- =====================================================
-- 3. RetryEmailHistory: ALTER 4 columns → NULL (ให้ตรง Dev)
-- =====================================================
PRINT '3. RetryEmailHistory - Change NOT NULL to NULL...'

-- 3.1 TrainingRequestId: NOT NULL → NULL
ALTER TABLE dbo.RetryEmailHistory ALTER COLUMN [TrainingRequestId] INT NULL
PRINT '   TrainingRequestId → NULL'

-- 3.2 RetryBy: NOT NULL → NULL
ALTER TABLE dbo.RetryEmailHistory ALTER COLUMN [RetryBy] NVARCHAR(510) NULL
PRINT '   RetryBy → NULL'

-- 3.3 RetryDate: NOT NULL → NULL
ALTER TABLE dbo.RetryEmailHistory ALTER COLUMN [RetryDate] DATETIME NULL
PRINT '   RetryDate → NULL'

-- 3.4 StatusAtRetry: NOT NULL → NULL
ALTER TABLE dbo.RetryEmailHistory ALTER COLUMN [StatusAtRetry] NVARCHAR(200) NULL
PRINT '   StatusAtRetry → NULL'
GO

PRINT '   RetryEmailHistory - Done (7 columns, nullable fixed)'
PRINT ''

-- =====================================================
-- 4. TrainingParticipants: DROP TABLE (Dev ไม่มี)
-- =====================================================
PRINT '4. TrainingParticipants - Drop table (Dev does not have this table)...'

IF OBJECT_ID('dbo.TrainingParticipants', 'U') IS NOT NULL
BEGIN
    -- Drop Foreign Keys ที่อ้างอิงตารางนี้ก่อน (ถ้ามี)
    DECLARE @fk_name NVARCHAR(200)
    DECLARE fk_cursor CURSOR FOR
        SELECT fk.name
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('dbo.TrainingParticipants')
           OR fk.referenced_object_id = OBJECT_ID('dbo.TrainingParticipants')

    OPEN fk_cursor
    FETCH NEXT FROM fk_cursor INTO @fk_name
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('ALTER TABLE dbo.TrainingParticipants DROP CONSTRAINT [' + @fk_name + ']')
        PRINT '   Dropped FK: ' + @fk_name
        FETCH NEXT FROM fk_cursor INTO @fk_name
    END
    CLOSE fk_cursor
    DEALLOCATE fk_cursor

    DROP TABLE dbo.TrainingParticipants
    PRINT '   Dropped table: TrainingParticipants'
END
ELSE
    PRINT '   TrainingParticipants - already not exists (OK)'
GO

PRINT ''

-- =====================================================
-- VERIFICATION: ตรวจสอบหลัง Migration
-- =====================================================
PRINT '=================================================================='
PRINT '  VERIFICATION: ตรวจสอบผลหลัง Migration'
PRINT '=================================================================='
PRINT ''

-- Check column counts
PRINT '--- Column Counts ---'
DECLARE @ExpectedCounts TABLE (TableName NVARCHAR(100), ExpectedCount INT)
INSERT INTO @ExpectedCounts VALUES
    ('ApprovalHistory', 9),
    ('EmailLogs', 10),
    ('RetryEmailHistory', 7),
    ('TrainingHistory', 8),
    ('TrainingRequest_Cost', 7),
    ('TrainingRequestAttachments', 4),
    ('TrainingRequestEmployees', 14),
    ('TrainingRequests', 81);

SELECT
    ec.TableName AS [Table_Name],
    ec.ExpectedCount AS [Expected],
    ISNULL(actual.cnt, 0) AS [Actual],
    CASE
        WHEN ISNULL(actual.cnt, 0) = 0 THEN 'TABLE NOT FOUND'
        WHEN ec.ExpectedCount = actual.cnt THEN 'OK'
        WHEN actual.cnt > ec.ExpectedCount THEN 'EXTRA +' + CAST(actual.cnt - ec.ExpectedCount AS VARCHAR)
        ELSE 'MISSING -' + CAST(ec.ExpectedCount - actual.cnt AS VARCHAR)
    END AS [Status]
FROM @ExpectedCounts ec
LEFT JOIN (
    SELECT t.name, COUNT(*) AS cnt
    FROM sys.tables t
    INNER JOIN sys.columns c ON t.object_id = c.object_id
    WHERE t.schema_id = SCHEMA_ID('dbo')
    GROUP BY t.name
) actual ON ec.TableName = actual.name
ORDER BY ec.TableName;

-- Check TrainingParticipants is gone
PRINT ''
PRINT '--- TrainingParticipants Check ---'
IF OBJECT_ID('dbo.TrainingParticipants', 'U') IS NULL
    PRINT '   TrainingParticipants: DROPPED (OK - matches Dev)'
ELSE
    PRINT '   TrainingParticipants: STILL EXISTS (ERROR!)'

-- Check EmailLogs nullable
PRINT ''
PRINT '--- EmailLogs Nullable Check ---'
SELECT
    c.name AS [Column_Name],
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS [Nullable],
    CASE
        WHEN c.name = 'RecipientEmail' AND c.is_nullable = 0 THEN 'OK'
        WHEN c.name = 'EmailType' AND c.is_nullable = 0 THEN 'OK'
        WHEN c.name = 'SentDate' AND c.is_nullable = 0 THEN 'OK'
        WHEN c.name = 'Status' AND c.is_nullable = 0 THEN 'OK'
        WHEN c.name IN ('RecipientEmail','EmailType','SentDate','Status') THEN 'MISMATCH!'
        ELSE 'N/A'
    END AS [Status]
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.EmailLogs')
AND c.name IN ('RecipientEmail', 'EmailType', 'SentDate', 'Status')
ORDER BY c.name;

-- Check RetryEmailHistory nullable
PRINT ''
PRINT '--- RetryEmailHistory Nullable Check ---'
SELECT
    c.name AS [Column_Name],
    CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END AS [Nullable],
    CASE
        WHEN c.name = 'TrainingRequestId' AND c.is_nullable = 1 THEN 'OK'
        WHEN c.name = 'RetryBy' AND c.is_nullable = 1 THEN 'OK'
        WHEN c.name = 'RetryDate' AND c.is_nullable = 1 THEN 'OK'
        WHEN c.name = 'StatusAtRetry' AND c.is_nullable = 1 THEN 'OK'
        WHEN c.name IN ('TrainingRequestId','RetryBy','RetryDate','StatusAtRetry') THEN 'MISMATCH!'
        ELSE 'N/A'
    END AS [Status]
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.RetryEmailHistory')
AND c.name IN ('TrainingRequestId', 'RetryBy', 'RetryDate', 'StatusAtRetry')
ORDER BY c.name;

PRINT ''
PRINT '=================================================================='
PRINT '  Migration Completed!'
PRINT '  Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
GO
