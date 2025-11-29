-- =====================================================
-- Create EmailLogs Table
-- Purpose: เก็บ Log การส่ง Email สำหรับ Debug และ Audit
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.EmailLogs', 'U') IS NOT NULL
--     DROP TABLE dbo.EmailLogs;
-- GO

CREATE TABLE [dbo].[EmailLogs] (
    -- Primary Key
    [Id] INT PRIMARY KEY IDENTITY(1,1),

    -- Training Request Reference
    [TrainingRequestId] INT NULL,
    [DocNo] NVARCHAR(20) NULL,

    -- Email Information
    [RecipientEmail] NVARCHAR(100) NULL,
    [EmailType] NVARCHAR(50) NULL,
    [Subject] NVARCHAR(200) NULL,

    -- Email Status
    [SentDate] DATETIME2(3) NULL DEFAULT GETDATE(),
    [Status] NVARCHAR(20) NULL,
    [ErrorMessage] NVARCHAR(1000) NULL,
    [RetryCount] INT NULL DEFAULT 0,

    -- Foreign Key Constraint
    CONSTRAINT [FK_EmailLogs_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE SET NULL
);
GO

-- Create Indexes for Performance
CREATE INDEX [IX_EmailLogs_TrainingRequestId]
    ON [dbo].[EmailLogs]([TrainingRequestId]);
GO

CREATE INDEX [IX_EmailLogs_DocNo]
    ON [dbo].[EmailLogs]([DocNo]);
GO

CREATE INDEX [IX_EmailLogs_Status]
    ON [dbo].[EmailLogs]([Status]);
GO

CREATE INDEX [IX_EmailLogs_SentDate]
    ON [dbo].[EmailLogs]([SentDate] DESC);
GO

CREATE INDEX [IX_EmailLogs_RecipientEmail]
    ON [dbo].[EmailLogs]([RecipientEmail]);
GO

PRINT '✅ EmailLogs table created successfully with indexes!';
GO
