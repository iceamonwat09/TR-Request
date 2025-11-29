-- =====================================================
-- Create TrainingRequestAttachments Table
-- Purpose: เก็บข้อมูลไฟล์แนบของคำขออบรม
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.TrainingRequestAttachments', 'U') IS NOT NULL
--     DROP TABLE dbo.TrainingRequestAttachments;
-- GO

CREATE TABLE [dbo].[TrainingRequestAttachments] (
    -- Primary Key
    [ID] INT PRIMARY KEY IDENTITY(1,1),

    -- Document Reference
    [DocNo] NVARCHAR(20) NULL,

    -- File Information
    [File_Name] NVARCHAR(255) NULL,
    [Modify_Date] NVARCHAR(50) NULL
);
GO

-- Create Indexes for Performance
CREATE INDEX [IX_TrainingRequestAttachments_DocNo]
    ON [dbo].[TrainingRequestAttachments]([DocNo]);
GO

CREATE INDEX [IX_TrainingRequestAttachments_FileName]
    ON [dbo].[TrainingRequestAttachments]([File_Name]);
GO

PRINT '✅ TrainingRequestAttachments table created successfully with indexes!';
GO
