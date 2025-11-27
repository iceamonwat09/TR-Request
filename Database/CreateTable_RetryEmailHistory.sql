-- =====================================================
-- Create RetryEmailHistory Table
-- Purpose: เก็บ Log การกด Retry Email โดย Admin/System Admin
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only)
-- IF OBJECT_ID('dbo.RetryEmailHistory', 'U') IS NOT NULL
--     DROP TABLE dbo.RetryEmailHistory;
-- GO

CREATE TABLE [dbo].[RetryEmailHistory] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [TrainingRequestId] INT NOT NULL,
    [DocNo] NVARCHAR(20) NOT NULL,
    [RetryBy] NVARCHAR(255) NOT NULL,
    -- Email ของ Admin ที่กด Retry
    [RetryDate] DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    [StatusAtRetry] NVARCHAR(100) NOT NULL,
    -- สถานะของเอกสารขณะที่กด Retry
    [ApproverEmail] NVARCHAR(255),
    -- Email ของผู้อนุมัติคนปัจจุบัน (ถ้ามี)
    [IPAddress] NVARCHAR(50),
    -- IP Address ของ Admin ที่กด Retry
    [Remark] NVARCHAR(500),
    -- หมายเหตุเพิ่มเติม (Optional)

    CONSTRAINT [FK_RetryEmailHistory_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE CASCADE
);
GO

-- Create Indexes
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

PRINT '✅ RetryEmailHistory table created successfully!';
GO
