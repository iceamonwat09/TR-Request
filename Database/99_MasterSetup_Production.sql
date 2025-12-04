-- =====================================================
-- MASTER PRODUCTION DATABASE SETUP SCRIPT
-- Database: HRDSYSTEM (Training Request System)
-- Description: Complete all-in-one setup script for production
-- Created: 2025-11-29
-- Usage: Execute this script on a fresh SQL Server to create
--        the complete database structure
-- =====================================================

USE [master]
GO

PRINT '=================================================='
PRINT 'HRDSYSTEM - Training Request System'
PRINT 'Production Database Setup'
PRINT 'Started: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================='
GO

-- =====================================================
-- Step 1: Create Database (if not exists)
-- =====================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HRDSYSTEM')
BEGIN
    CREATE DATABASE [HRDSYSTEM]
    PRINT '✅ Database HRDSYSTEM created successfully!'
END
ELSE
BEGIN
    PRINT '⚠️  Database HRDSYSTEM already exists.'
END
GO

USE [HRDSYSTEM]
GO

-- =====================================================
-- Step 2: Create TrainingRequests Table (Main Table)
-- =====================================================
IF NOT OBJECT_ID('dbo.TrainingRequests', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  TrainingRequests table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating TrainingRequests table...'

    CREATE TABLE [dbo].[TrainingRequests] (
        -- Primary Key
        [Id] INT PRIMARY KEY IDENTITY(1,1),

        -- Document Information
        [DocNo] NVARCHAR(20) NULL,
        [Company] NVARCHAR(50) NULL,
        [TrainingType] NVARCHAR(20) NULL,
        [Factory] NVARCHAR(100) NULL,

        -- Employee Information
        [CCEmail] NVARCHAR(250) NULL,
        [Position] NVARCHAR(100) NULL,
        [Department] NVARCHAR(100) NULL,
        [EmployeeCode] NVARCHAR(20) NULL,

        -- Training Schedule
        [StartDate] DATE NULL,
        [EndDate] DATE NULL,

        -- Training Details
        [SeminarTitle] NVARCHAR(200) NULL,
        [TrainingLocation] NVARCHAR(200) NULL,
        [Instructor] NVARCHAR(150) NULL,

        -- Cost Information
        [TotalCost] DECIMAL(12, 2) NULL DEFAULT 0,
        [CostPerPerson] DECIMAL(12, 2) NULL DEFAULT 0,
        [PerPersonTrainingHours] INT NULL DEFAULT 0,
        [RegistrationCost] DECIMAL(12, 2) NULL DEFAULT 0,
        [InstructorFee] DECIMAL(12, 2) NULL DEFAULT 0,
        [EquipmentCost] DECIMAL(12, 2) NULL DEFAULT 0,
        [FoodCost] DECIMAL(12, 2) NULL DEFAULT 0,
        [OtherCost] DECIMAL(12, 2) NULL DEFAULT 0,
        [OtherCostDescription] NVARCHAR(500) NULL,
        [TotalPeople] INT NULL DEFAULT 0,

        -- Training Purpose
        [TrainingObjective] NVARCHAR(100) NULL,
        [OtherObjective] NVARCHAR(500) NULL,
        [URLSource] NVARCHAR(500) NULL,
        [AdditionalNotes] NVARCHAR(1000) NULL,
        [ExpectedOutcome] NVARCHAR(1000) NULL,

        -- File Attachment
        [AttachedFilePath] NVARCHAR(500) NULL,

        -- Workflow Status
        [Status] NVARCHAR(50) NULL DEFAULT 'DRAFT',

        -- Section Manager Approval
        [SectionManagerId] NVARCHAR(100) NULL,
        [Status_SectionManager] NVARCHAR(20) NULL,
        [Comment_SectionManager] NVARCHAR(500) NULL,
        [ApproveInfo_SectionManager] NVARCHAR(200) NULL,

        -- Department Manager Approval
        [DepartmentManagerId] NVARCHAR(100) NULL,
        [Status_DepartmentManager] NVARCHAR(20) NULL,
        [Comment_DepartmentManager] NVARCHAR(500) NULL,
        [ApproveInfo_DepartmentManager] NVARCHAR(200) NULL,

        -- Managing Director Approval
        [ManagingDirectorId] NVARCHAR(100) NULL,
        [Status_ManagingDirector] NVARCHAR(20) NULL,
        [Comment_ManagingDirector] NVARCHAR(500) NULL,
        [ApproveInfo_ManagingDirector] NVARCHAR(200) NULL,

        -- HRD Admin Approval
        [HRDAdminid] NVARCHAR(100) NULL,
        [Status_HRDAdmin] NVARCHAR(20) NULL,
        [Comment_HRDAdmin] NVARCHAR(500) NULL,
        [ApproveInfo_HRDAdmin] NVARCHAR(200) NULL,

        -- HRD Confirmation
        [HRDConfirmationid] NVARCHAR(100) NULL,
        [Status_HRDConfirmation] NVARCHAR(20) NULL,
        [Comment_HRDConfirmation] NVARCHAR(500) NULL,
        [ApproveInfo_HRDConfirmation] NVARCHAR(200) NULL,

        -- Audit Fields
        [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedDate] DATETIME2(3) NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [IsActive] BIT NULL DEFAULT 1
    );

    -- Create Indexes
    CREATE INDEX [IX_TrainingRequests_DocNo] ON [dbo].[TrainingRequests]([DocNo]);
    CREATE INDEX [IX_TrainingRequests_Status] ON [dbo].[TrainingRequests]([Status]);
    CREATE INDEX [IX_TrainingRequests_Department] ON [dbo].[TrainingRequests]([Department]);
    CREATE INDEX [IX_TrainingRequests_CreatedDate] ON [dbo].[TrainingRequests]([CreatedDate] DESC);
    CREATE INDEX [IX_TrainingRequests_StartDate] ON [dbo].[TrainingRequests]([StartDate] DESC);
    CREATE INDEX [IX_TrainingRequests_CreatedBy] ON [dbo].[TrainingRequests]([CreatedBy]);
    CREATE INDEX [IX_TrainingRequests_IsActive] ON [dbo].[TrainingRequests]([IsActive]);

    PRINT '✅ TrainingRequests table created successfully!'
END
GO

-- =====================================================
-- Step 3: Create TrainingRequestEmployees Table
-- =====================================================
IF OBJECT_ID('dbo.TrainingRequestEmployees', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  TrainingRequestEmployees table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating TrainingRequestEmployees table...'

    CREATE TABLE [dbo].[TrainingRequestEmployees] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NULL,
        [EmployeeCode] NVARCHAR(20) NULL,
        [EmployeeName] NVARCHAR(100) NULL,
        [Position] NVARCHAR(100) NULL,
        [Department] NVARCHAR(100) NULL,
        [level] NVARCHAR(100) NULL,
        [PreviousTrainingHours] INT NULL DEFAULT 0,
        [CurrentTrainingHours] INT NULL DEFAULT 0,
        [RemainingHours] INT NULL DEFAULT 0,
        [PreviousTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0,
        [CurrentTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0,
        [RemainingCost] DECIMAL(10, 2) NULL DEFAULT 0,
        [Notes] NVARCHAR(500) NULL,

        CONSTRAINT [FK_TrainingRequestEmployees_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );

    -- Create Indexes
    CREATE INDEX [IX_TrainingRequestEmployees_TrainingRequestId] ON [dbo].[TrainingRequestEmployees]([TrainingRequestId]);
    CREATE INDEX [IX_TrainingRequestEmployees_EmployeeCode] ON [dbo].[TrainingRequestEmployees]([EmployeeCode]);
    CREATE INDEX [IX_TrainingRequestEmployees_Department] ON [dbo].[TrainingRequestEmployees]([Department]);

    PRINT '✅ TrainingRequestEmployees table created successfully!'
END
GO

-- =====================================================
-- Step 4: Create TrainingRequestAttachments Table
-- =====================================================
IF OBJECT_ID('dbo.TrainingRequestAttachments', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  TrainingRequestAttachments table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating TrainingRequestAttachments table...'

    CREATE TABLE [dbo].[TrainingRequestAttachments] (
        [ID] INT PRIMARY KEY IDENTITY(1,1),
        [DocNo] NVARCHAR(20) NULL,
        [File_Name] NVARCHAR(255) NULL,
        [Modify_Date] NVARCHAR(50) NULL
    );

    -- Create Indexes
    CREATE INDEX [IX_TrainingRequestAttachments_DocNo] ON [dbo].[TrainingRequestAttachments]([DocNo]);
    CREATE INDEX [IX_TrainingRequestAttachments_FileName] ON [dbo].[TrainingRequestAttachments]([File_Name]);

    PRINT '✅ TrainingRequestAttachments table created successfully!'
END
GO

-- =====================================================
-- Step 5: Create TrainingRequest_Cost Table
-- =====================================================
IF OBJECT_ID('dbo.TrainingRequest_Cost', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  TrainingRequest_Cost table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating TrainingRequest_Cost table...'

    CREATE TABLE [dbo].[TrainingRequest_Cost] (
        [ID] INT PRIMARY KEY IDENTITY(1,1),
        [Department] NVARCHAR(100) NULL,
        [Year] NVARCHAR(50) NULL,
        [Cost] DECIMAL(12, 2) NULL DEFAULT 0,
        [Qhours] INT NULL DEFAULT 0,
        [CreatedBy] NVARCHAR(100) NULL
    );

    -- Create Indexes
    CREATE INDEX [IX_TrainingRequest_Cost_Department] ON [dbo].[TrainingRequest_Cost]([Department]);
    CREATE INDEX [IX_TrainingRequest_Cost_Year] ON [dbo].[TrainingRequest_Cost]([Year]);
    CREATE INDEX [IX_TrainingRequest_Cost_Department_Year] ON [dbo].[TrainingRequest_Cost]([Department], [Year]);

    PRINT '✅ TrainingRequest_Cost table created successfully!'
END
GO

-- =====================================================
-- Step 6: Create RetryEmailHistory Table
-- =====================================================
IF OBJECT_ID('dbo.RetryEmailHistory', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  RetryEmailHistory table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating RetryEmailHistory table...'

    CREATE TABLE [dbo].[RetryEmailHistory] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NOT NULL,
        [DocNo] NVARCHAR(50) NULL,
        [RetryBy] NVARCHAR(255) NOT NULL,
        [RetryDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [StatusAtRetry] NVARCHAR(100) NOT NULL,
        [IPAddress] NVARCHAR(50) NULL,

        CONSTRAINT [FK_RetryEmailHistory_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );

    -- Create Indexes
    CREATE INDEX [IX_RetryEmailHistory_TrainingRequestId] ON [dbo].[RetryEmailHistory]([TrainingRequestId]);
    CREATE INDEX [IX_RetryEmailHistory_DocNo] ON [dbo].[RetryEmailHistory]([DocNo]);
    CREATE INDEX [IX_RetryEmailHistory_RetryBy] ON [dbo].[RetryEmailHistory]([RetryBy]);
    CREATE INDEX [IX_RetryEmailHistory_RetryDate] ON [dbo].[RetryEmailHistory]([RetryDate] DESC);

    PRINT '✅ RetryEmailHistory table created successfully!'
END
GO

-- =====================================================
-- Step 7: Create EmailLogs Table
-- =====================================================
IF OBJECT_ID('dbo.EmailLogs', 'U') IS NOT NULL
BEGIN
    PRINT '⚠️  EmailLogs table already exists. Skipping...'
END
ELSE
BEGIN
    PRINT 'Creating EmailLogs table...'

    CREATE TABLE [dbo].[EmailLogs] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NULL,
        [DocNo] NVARCHAR(20) NULL,
        [RecipientEmail] NVARCHAR(100) NULL,
        [EmailType] NVARCHAR(50) NULL,
        [Subject] NVARCHAR(200) NULL,
        [SentDate] DATETIME2(3) NULL DEFAULT GETDATE(),
        [Status] NVARCHAR(20) NULL,
        [ErrorMessage] NVARCHAR(1000) NULL,
        [RetryCount] INT NULL DEFAULT 0,

        CONSTRAINT [FK_EmailLogs_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE SET NULL
    );

    -- Create Indexes
    CREATE INDEX [IX_EmailLogs_TrainingRequestId] ON [dbo].[EmailLogs]([TrainingRequestId]);
    CREATE INDEX [IX_EmailLogs_DocNo] ON [dbo].[EmailLogs]([DocNo]);
    CREATE INDEX [IX_EmailLogs_Status] ON [dbo].[EmailLogs]([Status]);
    CREATE INDEX [IX_EmailLogs_SentDate] ON [dbo].[EmailLogs]([SentDate] DESC);
    CREATE INDEX [IX_EmailLogs_RecipientEmail] ON [dbo].[EmailLogs]([RecipientEmail]);

    PRINT '✅ EmailLogs table created successfully!'
END
GO

-- =====================================================
-- Verification: Check All Tables Created
-- =====================================================
PRINT ''
PRINT '=================================================='
PRINT 'Verification: Checking all tables...'
PRINT '=================================================='

DECLARE @TableCount INT
SELECT @TableCount = COUNT(*)
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

PRINT 'Total tables created: ' + CAST(@TableCount AS VARCHAR(10)) + '/6'

IF @TableCount = 6
BEGIN
    PRINT '✅ All tables created successfully!'
END
ELSE
BEGIN
    PRINT '⚠️  Warning: Some tables may not have been created.'
END

-- List all tables
PRINT ''
PRINT 'Tables in database:'
SELECT TABLE_NAME,
       (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME) AND index_id > 0) as IndexCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME
GO

PRINT ''
PRINT '=================================================='
PRINT 'Database Setup Completed!'
PRINT 'Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================='
GO
