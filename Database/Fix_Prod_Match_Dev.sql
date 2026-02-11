-- =====================================================
-- Script: Fix Prod to Match Dev Schema (V2)
-- Purpose: แก้ไข Prod ให้ตรงกับ Dev (Dev เป็นหลัก)
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-11
-- Note: ลบ ALL constraints (default, index, FK) ก่อน DROP COLUMN
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '========================================================'
PRINT '  Fix Prod to Match Dev Schema (V2)'
PRINT '  Start Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
PRINT ''

-- =====================================================
-- 1. ลบ ApprovalHistory.DocNo
--    ลบ constraints ทุกประเภทก่อน แล้วค่อย DROP COLUMN
-- =====================================================
PRINT '--- Step 1: Remove ApprovalHistory.DocNo ---'

-- 1a. ลบ Default Constraints
DECLARE @sql NVARCHAR(MAX) = ''
SELECT @sql = @sql + 'ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + dc.name + ']; '
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'DocNo'

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql
    PRINT 'DROPPED  >> Default Constraints on ApprovalHistory.DocNo'
END

-- 1b. ลบ Check Constraints
SET @sql = ''
SELECT @sql = @sql + 'ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + cc.name + ']; '
FROM sys.check_constraints cc
INNER JOIN sys.columns c ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
WHERE cc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'DocNo'

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql
    PRINT 'DROPPED  >> Check Constraints on ApprovalHistory.DocNo'
END

-- 1c. ลบ Indexes ที่มี column DocNo
SET @sql = ''
SELECT @sql = @sql + 'DROP INDEX [' + i.name + '] ON [dbo].[ApprovalHistory]; '
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'DocNo'
AND i.is_primary_key = 0 AND i.is_unique_constraint = 0

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql
    PRINT 'DROPPED  >> Indexes on ApprovalHistory.DocNo'
END

-- 1d. ลบ FK Constraints ที่อ้างถึง DocNo
SET @sql = ''
SELECT @sql = @sql + 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.parent_object_id) + '].[' + OBJECT_NAME(fk.parent_object_id) + '] DROP CONSTRAINT [' + fk.name + ']; '
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE (fkc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'DocNo')
   OR (fkc.referenced_object_id = OBJECT_ID('dbo.ApprovalHistory') AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'DocNo')

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql
    PRINT 'DROPPED  >> FK Constraints on ApprovalHistory.DocNo'
END

-- 1e. DROP COLUMN
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] DROP COLUMN [DocNo];
    PRINT 'DROPPED  >> ApprovalHistory.DocNo'
END
ELSE PRINT 'SKIP     >> ApprovalHistory.DocNo - not found'
GO

-- =====================================================
-- 2. ลบ ApprovalHistory.IpAddress
-- =====================================================
PRINT ''
PRINT '--- Step 2: Remove ApprovalHistory.IpAddress ---'

-- 2a. ลบ Default Constraints
DECLARE @sql2 NVARCHAR(MAX) = ''
SELECT @sql2 = @sql2 + 'ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + dc.name + ']; '
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'IpAddress'

IF @sql2 <> ''
BEGIN
    EXEC sp_executesql @sql2
    PRINT 'DROPPED  >> Default Constraints on ApprovalHistory.IpAddress'
END

-- 2b. ลบ Check Constraints
SET @sql2 = ''
SELECT @sql2 = @sql2 + 'ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + cc.name + ']; '
FROM sys.check_constraints cc
INNER JOIN sys.columns c ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
WHERE cc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'IpAddress'

IF @sql2 <> ''
BEGIN
    EXEC sp_executesql @sql2
    PRINT 'DROPPED  >> Check Constraints on ApprovalHistory.IpAddress'
END

-- 2c. ลบ Indexes
SET @sql2 = ''
SELECT @sql2 = @sql2 + 'DROP INDEX [' + i.name + '] ON [dbo].[ApprovalHistory]; '
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'IpAddress'
AND i.is_primary_key = 0 AND i.is_unique_constraint = 0

IF @sql2 <> ''
BEGIN
    EXEC sp_executesql @sql2
    PRINT 'DROPPED  >> Indexes on ApprovalHistory.IpAddress'
END

-- 2d. ลบ FK Constraints
SET @sql2 = ''
SELECT @sql2 = @sql2 + 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.parent_object_id) + '].[' + OBJECT_NAME(fk.parent_object_id) + '] DROP CONSTRAINT [' + fk.name + ']; '
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE (fkc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'IpAddress')
   OR (fkc.referenced_object_id = OBJECT_ID('dbo.ApprovalHistory') AND COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) = 'IpAddress')

IF @sql2 <> ''
BEGIN
    EXEC sp_executesql @sql2
    PRINT 'DROPPED  >> FK Constraints on ApprovalHistory.IpAddress'
END

-- 2e. DROP COLUMN
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'IpAddress')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] DROP COLUMN [IpAddress];
    PRINT 'DROPPED  >> ApprovalHistory.IpAddress'
END
ELSE PRINT 'SKIP     >> ApprovalHistory.IpAddress - not found'
GO

-- =====================================================
-- 3. Drop ตาราง TrainingParticipants
-- =====================================================
PRINT ''
PRINT '--- Step 3: Drop TrainingParticipants table ---'

IF OBJECT_ID('dbo.TrainingParticipants', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[TrainingParticipants];
    PRINT 'DROPPED  >> Table [dbo].[TrainingParticipants]'
END
ELSE PRINT 'SKIP     >> Table TrainingParticipants - not found'
GO

-- =====================================================
-- 4. Rename HRDAdminId -> HRDAdminid
-- =====================================================
PRINT ''
PRINT '--- Step 4: Rename HRDAdminId -> HRDAdminid ---'

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDAdminId')
BEGIN
    EXEC sp_rename 'dbo.TrainingRequests.HRDAdminId', 'HRDAdminid', 'COLUMN';
    PRINT 'RENAMED  >> TrainingRequests.HRDAdminId -> HRDAdminid'
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDAdminid')
    PRINT 'SKIP     >> TrainingRequests.HRDAdminid - already correct'
ELSE
    PRINT 'WARNING  >> TrainingRequests.HRDAdminId/HRDAdminid - column not found!'
GO

-- =====================================================
-- 5. Rename HRDConfirmationId -> HRDConfirmationid
-- =====================================================
PRINT ''
PRINT '--- Step 5: Rename HRDConfirmationId -> HRDConfirmationid ---'

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDConfirmationId')
BEGIN
    EXEC sp_rename 'dbo.TrainingRequests.HRDConfirmationId', 'HRDConfirmationid', 'COLUMN';
    PRINT 'RENAMED  >> TrainingRequests.HRDConfirmationId -> HRDConfirmationid'
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDConfirmationid')
    PRINT 'SKIP     >> TrainingRequests.HRDConfirmationid - already correct'
ELSE
    PRINT 'WARNING  >> TrainingRequests.HRDConfirmationId/HRDConfirmationid - column not found!'
GO

-- =====================================================
-- Verification
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '--- Verification ---'
PRINT '========================================================'
PRINT ''

PRINT 'Expected (match Dev):'
PRINT '  ApprovalHistory         = 9'
PRINT '  EmailLogs               = 10'
PRINT '  RetryEmailHistory       = 7'
PRINT '  TrainingHistory         = 8'
PRINT '  TrainingRequest_Cost    = 7'
PRINT '  TrainingRequestAttachments = 4'
PRINT '  TrainingRequestEmployees   = 14'
PRINT '  TrainingRequests        = 81'
PRINT ''

SELECT
    t.name AS [Table_Name],
    COUNT(c.name) AS [Column_Count]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.name IN (
    'TrainingRequests',
    'TrainingRequestEmployees',
    'TrainingRequestAttachments',
    'TrainingRequest_Cost',
    'RetryEmailHistory',
    'EmailLogs',
    'ApprovalHistory',
    'TrainingHistory'
)
GROUP BY t.name
ORDER BY t.name;

-- แสดง columns ที่เหลือใน ApprovalHistory เพื่อตรวจสอบ
PRINT ''
PRINT '--- ApprovalHistory Columns (should be 9) ---'
SELECT c.name AS ColumnName, t.name AS DataType, c.max_length
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.ApprovalHistory')
ORDER BY c.column_id;

PRINT ''
PRINT '========================================================'
PRINT '  Fix Script V2 Completed!'
PRINT '  End Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
GO
