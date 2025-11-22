-- =====================================================
-- Create EmailLogs Table (Optional)
-- Purpose: เก็บ Log การส่ง Email สำหรับ Debug
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only)
-- IF OBJECT_ID('dbo.EmailLogs', 'U') IS NOT NULL
--     DROP TABLE dbo.EmailLogs;
-- GO

CREATE TABLE [dbo].[EmailLogs] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [TrainingRequestId] INT,
    [DocNo] NVARCHAR(20),
    [RecipientEmail] NVARCHAR(100) NOT NULL,
    [EmailType] NVARCHAR(50) NOT NULL,
    -- Values: PENDING_NOTIFICATION, APPROVAL_REQUEST, APPROVAL_NOTIFICATION, REVISE_NOTIFICATION, etc.
    [Subject] NVARCHAR(200),
    [SentDate] DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    [Status] NVARCHAR(20) NOT NULL,
    -- Values: SENT, FAILED
    [ErrorMessage] NVARCHAR(1000),
    [RetryCount] INT DEFAULT 0,

    CONSTRAINT [FK_EmailLogs_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE SET NULL
);
GO

-- Create Indexes
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

PRINT '✅ EmailLogs table created successfully!';
GO
