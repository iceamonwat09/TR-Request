-- =====================================================
-- Create TrainingRequests Table (Main Table)
-- Purpose: เก็บข้อมูลคำขออบรมหลัก
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NOT NULL
--     DROP TABLE dbo.TrainingRequests;
-- GO

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
GO

-- Create Indexes for Performance
CREATE INDEX [IX_TrainingRequests_DocNo]
    ON [dbo].[TrainingRequests]([DocNo]);
GO

CREATE INDEX [IX_TrainingRequests_Status]
    ON [dbo].[TrainingRequests]([Status]);
GO

CREATE INDEX [IX_TrainingRequests_Department]
    ON [dbo].[TrainingRequests]([Department]);
GO

CREATE INDEX [IX_TrainingRequests_CreatedDate]
    ON [dbo].[TrainingRequests]([CreatedDate] DESC);
GO

CREATE INDEX [IX_TrainingRequests_StartDate]
    ON [dbo].[TrainingRequests]([StartDate] DESC);
GO

CREATE INDEX [IX_TrainingRequests_CreatedBy]
    ON [dbo].[TrainingRequests]([CreatedBy]);
GO

CREATE INDEX [IX_TrainingRequests_IsActive]
    ON [dbo].[TrainingRequests]([IsActive]);
GO

PRINT '✅ TrainingRequests table created successfully with indexes!';
GO
