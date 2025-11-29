-- =====================================================
-- Create TrainingRequestEmployees Table
-- Purpose: เก็บข้อมูลพนักงานที่เข้าร่วมอบรม
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.TrainingRequestEmployees', 'U') IS NOT NULL
--     DROP TABLE dbo.TrainingRequestEmployees;
-- GO

CREATE TABLE [dbo].[TrainingRequestEmployees] (
    -- Primary Key
    [Id] INT PRIMARY KEY IDENTITY(1,1),

    -- Foreign Key to TrainingRequests
    [TrainingRequestId] INT NULL,

    -- Employee Information
    [EmployeeCode] NVARCHAR(20) NULL,
    [EmployeeName] NVARCHAR(100) NULL,
    [Position] NVARCHAR(100) NULL,
    [Department] NVARCHAR(100) NULL,
    [level] NVARCHAR(100) NULL,

    -- Training Hours Tracking
    [PreviousTrainingHours] INT NULL DEFAULT 0,
    [CurrentTrainingHours] INT NULL DEFAULT 0,
    [RemainingHours] INT NULL DEFAULT 0,

    -- Training Cost Tracking
    [PreviousTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0,
    [CurrentTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0,
    [RemainingCost] DECIMAL(10, 2) NULL DEFAULT 0,

    -- Additional Information
    [Notes] NVARCHAR(500) NULL,

    -- Foreign Key Constraint
    CONSTRAINT [FK_TrainingRequestEmployees_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE CASCADE
);
GO

-- Create Indexes for Performance
CREATE INDEX [IX_TrainingRequestEmployees_TrainingRequestId]
    ON [dbo].[TrainingRequestEmployees]([TrainingRequestId]);
GO

CREATE INDEX [IX_TrainingRequestEmployees_EmployeeCode]
    ON [dbo].[TrainingRequestEmployees]([EmployeeCode]);
GO

CREATE INDEX [IX_TrainingRequestEmployees_Department]
    ON [dbo].[TrainingRequestEmployees]([Department]);
GO

PRINT '✅ TrainingRequestEmployees table created successfully with indexes!';
GO
