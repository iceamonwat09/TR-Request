-- =====================================================
-- Script: Fix Prod to Match Dev Schema
-- Purpose: แก้ไข Prod ให้ตรงกับ Dev (Dev เป็นหลัก)
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-11
-- =====================================================
-- สิ่งที่ต้องแก้:
-- 1. ลบ ApprovalHistory.DocNo (Dev ไม่มี)
-- 2. ลบ ApprovalHistory.IpAddress (Dev ไม่มี)
-- 3. Drop ตาราง TrainingParticipants (Dev ไม่มี)
-- 4. Rename HRDAdminId -> HRDAdminid (ตาม Dev)
-- 5. Rename HRDConfirmationId -> HRDConfirmationid (ตาม Dev)
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '========================================================'
PRINT '  Fix Prod to Match Dev Schema'
PRINT '  Start Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
PRINT ''

-- =====================================================
-- 1. ลบ ApprovalHistory.DocNo
-- =====================================================
PRINT '--- Step 1: Remove ApprovalHistory.DocNo ---'

-- ลบ Default Constraint ถ้ามี
DECLARE @ConstraintName1 NVARCHAR(200)
SELECT @ConstraintName1 = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'DocNo'

IF @ConstraintName1 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + @ConstraintName1 + ']')
    PRINT 'DROPPED  >> Default Constraint: ' + @ConstraintName1
END

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

DECLARE @ConstraintName2 NVARCHAR(200)
SELECT @ConstraintName2 = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.ApprovalHistory') AND c.name = 'IpAddress'

IF @ConstraintName2 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[ApprovalHistory] DROP CONSTRAINT [' + @ConstraintName2 + ']')
    PRINT 'DROPPED  >> Default Constraint: ' + @ConstraintName2
END

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
-- Verification: Compare column counts
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '--- Verification: Column Counts After Fix ---'
PRINT '========================================================'
PRINT ''

PRINT 'Expected (match Dev):'
PRINT '  ApprovalHistory         = 9'
PRINT '  EmailLogs               = 10'
PRINT '  RetryEmailHistory       = 7'
PRINT '  TrainingHistory         = 8'
PRINT '  TrainingParticipants    = (should not exist)'
PRINT '  TrainingRequest_Cost    = 7'
PRINT '  TrainingRequestAttachments = 4'
PRINT '  TrainingRequestEmployees   = 14'
PRINT '  TrainingRequests        = 81'
PRINT ''
PRINT 'Actual:'

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
    'TrainingHistory',
    'TrainingParticipants'
)
GROUP BY t.name
ORDER BY t.name;

-- Check TrainingParticipants should not exist
IF OBJECT_ID('dbo.TrainingParticipants', 'U') IS NULL
    PRINT 'OK       >> TrainingParticipants table does not exist (match Dev)'
ELSE
    PRINT 'WARNING  >> TrainingParticipants table still exists!'

-- Check column name casing
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDAdminid' COLLATE Latin1_General_CS_AS)
    PRINT 'OK       >> HRDAdminid casing is correct'
ELSE
    PRINT 'WARNING  >> HRDAdminid casing mismatch!'

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDConfirmationid' COLLATE Latin1_General_CS_AS)
    PRINT 'OK       >> HRDConfirmationid casing is correct'
ELSE
    PRINT 'WARNING  >> HRDConfirmationid casing mismatch!'

PRINT ''
PRINT '========================================================'
PRINT '  Fix Script Completed!'
PRINT '  End Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
GO
