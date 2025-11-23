-- =====================================================
-- Create ApprovalHistory Table
-- Purpose: เก็บประวัติการอนุมัติทุกขั้นตอน
-- =====================================================

USE [HRDSYSTEM]
GO

-- Drop table if exists (for development only)
-- IF OBJECT_ID('dbo.ApprovalHistory', 'U') IS NOT NULL
--     DROP TABLE dbo.ApprovalHistory;
-- GO

CREATE TABLE [dbo].[ApprovalHistory] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [TrainingRequestId] INT NOT NULL,
    [DocNo] NVARCHAR(20),
    [ApproverRole] NVARCHAR(50) NOT NULL,
    -- Values: SectionManager, DepartmentManager, HRDAdmin, HRDConfirmation, ManagingDirector
    [ApproverEmail] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(20) NOT NULL,
    -- Values: APPROVED, Revise, REJECTED
    [Comment] NVARCHAR(500),
    [ActionDate] DATETIME2(3) NOT NULL DEFAULT GETDATE(),
    [PreviousStatus] NVARCHAR(50),
    [NewStatus] NVARCHAR(50),
    [IpAddress] NVARCHAR(50),

    CONSTRAINT [FK_ApprovalHistory_TrainingRequests]
        FOREIGN KEY ([TrainingRequestId])
        REFERENCES [dbo].[TrainingRequests]([Id])
        ON DELETE CASCADE
);
GO

-- Create Indexes for performance
CREATE INDEX [IX_ApprovalHistory_TrainingRequestId]
    ON [dbo].[ApprovalHistory]([TrainingRequestId]);
GO

CREATE INDEX [IX_ApprovalHistory_DocNo]
    ON [dbo].[ApprovalHistory]([DocNo]);
GO

CREATE INDEX [IX_ApprovalHistory_ApproverEmail]
    ON [dbo].[ApprovalHistory]([ApproverEmail]);
GO

CREATE INDEX [IX_ApprovalHistory_ActionDate]
    ON [dbo].[ApprovalHistory]([ActionDate] DESC);
GO

PRINT '✅ ApprovalHistory table created successfully!';
GO
