-- =====================================================
-- Database Rollback Script
-- Purpose: Remove all tables (USE WITH EXTREME CAUTION!)
-- WARNING: This will delete ALL data in the tables
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '=================================================='
PRINT '⚠️  WARNING: DATABASE ROLLBACK SCRIPT'
PRINT '=================================================='
PRINT 'This script will DROP all tables and data!'
PRINT 'Press Ctrl+C to cancel, or wait 10 seconds...'
PRINT '=================================================='
WAITFOR DELAY '00:00:10'
GO

PRINT ''
PRINT 'Starting rollback...'
GO

-- =====================================================
-- Drop Foreign Keys First
-- =====================================================
PRINT ''
PRINT 'Step 1: Dropping Foreign Keys...'

-- Drop FK from TrainingRequestEmployees
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TrainingRequestEmployees_TrainingRequests')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees]
        DROP CONSTRAINT [FK_TrainingRequestEmployees_TrainingRequests]
    PRINT '   ✅ Dropped FK_TrainingRequestEmployees_TrainingRequests'
END

-- Drop FK from RetryEmailHistory
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RetryEmailHistory_TrainingRequests')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory]
        DROP CONSTRAINT [FK_RetryEmailHistory_TrainingRequests]
    PRINT '   ✅ Dropped FK_RetryEmailHistory_TrainingRequests'
END

-- Drop FK from EmailLogs
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EmailLogs_TrainingRequests')
BEGIN
    ALTER TABLE [dbo].[EmailLogs]
        DROP CONSTRAINT [FK_EmailLogs_TrainingRequests]
    PRINT '   ✅ Dropped FK_EmailLogs_TrainingRequests'
END

GO

-- =====================================================
-- Drop Tables in Correct Order
-- =====================================================
PRINT ''
PRINT 'Step 2: Dropping Tables...'

-- Drop dependent tables first
IF OBJECT_ID('dbo.TrainingRequestEmployees', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[TrainingRequestEmployees]
    PRINT '   ✅ Dropped TrainingRequestEmployees'
END

IF OBJECT_ID('dbo.TrainingRequestAttachments', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[TrainingRequestAttachments]
    PRINT '   ✅ Dropped TrainingRequestAttachments'
END

IF OBJECT_ID('dbo.RetryEmailHistory', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[RetryEmailHistory]
    PRINT '   ✅ Dropped RetryEmailHistory'
END

IF OBJECT_ID('dbo.EmailLogs', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[EmailLogs]
    PRINT '   ✅ Dropped EmailLogs'
END

IF OBJECT_ID('dbo.TrainingRequest_Cost', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[TrainingRequest_Cost]
    PRINT '   ✅ Dropped TrainingRequest_Cost'
END

-- Drop main table last
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[TrainingRequests]
    PRINT '   ✅ Dropped TrainingRequests'
END

GO

-- =====================================================
-- Verify Cleanup
-- =====================================================
PRINT ''
PRINT 'Step 3: Verifying cleanup...'

DECLARE @RemainingTables INT
SELECT @RemainingTables = COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
AND TABLE_NAME IN (
    'TrainingRequests',
    'TrainingRequestEmployees',
    'TrainingRequestAttachments',
    'TrainingRequest_Cost',
    'RetryEmailHistory',
    'EmailLogs'
)

IF @RemainingTables = 0
BEGIN
    PRINT '   ✅ All tables dropped successfully!'
END
ELSE
BEGIN
    PRINT '   ⚠️  Warning: ' + CAST(@RemainingTables AS VARCHAR(10)) + ' tables still exist'
    PRINT ''
    PRINT 'Remaining tables:'
    SELECT TABLE_NAME
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo'
    AND TABLE_NAME IN (
        'TrainingRequests',
        'TrainingRequestEmployees',
        'TrainingRequestAttachments',
        'TrainingRequest_Cost',
        'RetryEmailHistory',
        'EmailLogs'
    )
END

GO

PRINT ''
PRINT '=================================================='
PRINT 'Rollback Completed!'
PRINT 'Database is now empty and ready for fresh setup.'
PRINT '=================================================='
GO

-- =====================================================
-- Optional: Drop Database (COMMENTED OUT FOR SAFETY)
-- =====================================================
/*
USE [master]
GO

IF DB_ID('HRDSYSTEM') IS NOT NULL
BEGIN
    ALTER DATABASE [HRDSYSTEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    DROP DATABASE [HRDSYSTEM]
    PRINT '✅ Database HRDSYSTEM dropped completely!'
END
GO
*/
