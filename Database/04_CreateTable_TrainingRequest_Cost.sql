-- =====================================================
-- Create TrainingRequest_Cost Table
-- Purpose: เก็บข้อมูลโควต้างบประมาณและชั่วโมงอบรมของแต่ละฝ่าย
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.TrainingRequest_Cost', 'U') IS NOT NULL
--     DROP TABLE dbo.TrainingRequest_Cost;
-- GO

CREATE TABLE [dbo].[TrainingRequest_Cost] (
    -- Primary Key
    [ID] INT PRIMARY KEY IDENTITY(1,1),

    -- Department Budget Information
    [Department] NVARCHAR(100) NULL,
    [Year] NVARCHAR(50) NULL,

    -- Quota Information
    [Cost] DECIMAL(12, 2) NULL DEFAULT 0,
    [Qhours] INT NULL DEFAULT 0,

    -- Audit Fields
    [CreatedBy] NVARCHAR(100) NULL
);
GO

-- Create Indexes for Performance
CREATE INDEX [IX_TrainingRequest_Cost_Department]
    ON [dbo].[TrainingRequest_Cost]([Department]);
GO

CREATE INDEX [IX_TrainingRequest_Cost_Year]
    ON [dbo].[TrainingRequest_Cost]([Year]);
GO

-- Create Composite Index for Department + Year lookup
CREATE INDEX [IX_TrainingRequest_Cost_Department_Year]
    ON [dbo].[TrainingRequest_Cost]([Department], [Year]);
GO

PRINT '✅ TrainingRequest_Cost table created successfully with indexes!';
GO
