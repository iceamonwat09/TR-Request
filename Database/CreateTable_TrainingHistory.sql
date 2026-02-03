-- =====================================================
-- Create TrainingHistory Table
-- Purpose: เก็บข้อมูลประวัติการอบรมของพนักงาน (HRD Section)
-- Date: 2026-01-28
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '======================================='
PRINT 'Starting: Create TrainingHistory Table'
PRINT '======================================='
PRINT ''

-- ตรวจสอบว่าตาราง TrainingRequests มีอยู่หรือไม่
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NULL
BEGIN
    PRINT '❌ ERROR: Table [dbo].[TrainingRequests] does not exist!'
    PRINT 'Please run 01_CreateTable_TrainingRequests.sql first.'
    RETURN
END

-- สร้างตาราง TrainingHistory ถ้ายังไม่มี
IF OBJECT_ID('dbo.TrainingHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingHistory] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),

        -- Foreign Key to TrainingRequests
        [TrainingRequestId] INT NOT NULL,

        -- Employee Information
        [EmployeeCode] NVARCHAR(20) NULL,
        [EmployeeName] NVARCHAR(100) NULL,

        -- History Type: Never(ไม่เคย), Ever(เคย), Similar(ใกล้เคียง)
        [HistoryType] NVARCHAR(20) NULL,

        -- Training Details
        [TrainingDate] DATE NULL,
        [CourseName] NVARCHAR(500) NULL,

        -- Audit
        [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE(),

        -- Foreign Key Constraint
        CONSTRAINT [FK_TrainingHistory_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );

    PRINT '✅ Table [dbo].[TrainingHistory] created successfully'
END
ELSE
BEGIN
    PRINT '⚠️  Table [dbo].[TrainingHistory] already exists, skipping...'
END

-- Create Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_TrainingRequestId' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
BEGIN
    CREATE INDEX [IX_TrainingHistory_TrainingRequestId]
        ON [dbo].[TrainingHistory]([TrainingRequestId]);
    PRINT '✅ Index IX_TrainingHistory_TrainingRequestId created'
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_EmployeeCode' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
BEGIN
    CREATE INDEX [IX_TrainingHistory_EmployeeCode]
        ON [dbo].[TrainingHistory]([EmployeeCode]);
    PRINT '✅ Index IX_TrainingHistory_EmployeeCode created'
END

PRINT ''
PRINT '======================================='
PRINT '✅ TrainingHistory Table Setup Complete!'
PRINT '======================================='
PRINT ''
PRINT 'Columns:'
PRINT '  - Id (PK, IDENTITY)'
PRINT '  - TrainingRequestId (FK → TrainingRequests, CASCADE DELETE)'
PRINT '  - EmployeeCode, EmployeeName'
PRINT '  - HistoryType (Never/Ever/Similar)'
PRINT '  - TrainingDate, CourseName'
PRINT '  - CreatedDate'
PRINT ''

GO
