-- =====================================================
-- Create RetryEmailHistory Table
-- Purpose: เก็บ Log การกด Retry Email โดย Admin/System Admin
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only - comment out for production)
-- IF OBJECT_ID('dbo.RetryEmailHistory', 'U') IS NOT NULL
--     DROP TABLE dbo.RetryEmailHistory;
-- GO

CREATE TABLE [dbo].[RetryEmailHistory] (
    -- Primary Key
    [Id] INT PRIMARY KEY IDENTITY(1,1),

    -- Training Request Reference
    [TrainingRequestId] INT NOT NULL,
    [DocNo] NVARCHAR(50) NULL,

    -- Retry Information
    [RetryBy] NVARCHAR(255) NOT NULL,
    [RetryDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [StatusAtRetry] NVARCHAR(100) NOT NULL,

    -- Additional Information
    [IPAddress] NVARCHAR(50) NULL,

    -- Foreign Key Constraint
    CONSTRAINT [FK_RetryEmailHistory_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE CASCADE
);
GO

-- Create Indexes for Performance
CREATE INDEX [IX_RetryEmailHistory_TrainingRequestId]
    ON [dbo].[RetryEmailHistory]([TrainingRequestId]);
GO

CREATE INDEX [IX_RetryEmailHistory_DocNo]
    ON [dbo].[RetryEmailHistory]([DocNo]);
GO

CREATE INDEX [IX_RetryEmailHistory_RetryBy]
    ON [dbo].[RetryEmailHistory]([RetryBy]);
GO

CREATE INDEX [IX_RetryEmailHistory_RetryDate]
    ON [dbo].[RetryEmailHistory]([RetryDate] DESC);
GO

PRINT '✅ RetryEmailHistory table created successfully with indexes!';
GO
