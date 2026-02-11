-- =====================================================
-- Script: Check & Add All Columns for HRDSYSTEM
-- Purpose: ตรวจสอบ Column ทั้งหมดในทุกตาราง
--          ถ้ามีแล้วให้ข้ามไป ถ้าไม่มีให้เพิ่มให้อัตโนมัติ
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-11
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '========================================================'
PRINT '  HRDSYSTEM - Check & Add All Columns Script'
PRINT '  Start Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
PRINT ''

-- =====================================================
-- SECTION 1: ตรวจสอบและสร้างตารางที่ยังไม่มี
-- =====================================================
PRINT '--- SECTION 1: Check & Create Tables ---'
PRINT ''

-- 1.1 TrainingRequests (Main Table)
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingRequests] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [DocNo] NVARCHAR(20) NULL,
        [Company] NVARCHAR(50) NULL,
        [TrainingType] NVARCHAR(20) NULL,
        [Factory] NVARCHAR(100) NULL,
        [CCEmail] NVARCHAR(250) NULL,
        [Position] NVARCHAR(100) NULL,
        [Department] NVARCHAR(100) NULL,
        [EmployeeCode] NVARCHAR(20) NULL,
        [StartDate] DATE NULL,
        [EndDate] DATE NULL,
        [SeminarTitle] NVARCHAR(200) NULL,
        [TrainingLocation] NVARCHAR(200) NULL,
        [Instructor] NVARCHAR(150) NULL,
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
        [TrainingObjective] NVARCHAR(100) NULL,
        [OtherObjective] NVARCHAR(500) NULL,
        [URLSource] NVARCHAR(500) NULL,
        [AdditionalNotes] NVARCHAR(1000) NULL,
        [ExpectedOutcome] NVARCHAR(1000) NULL,
        [AttachedFilePath] NVARCHAR(500) NULL,
        [Status] NVARCHAR(50) NULL DEFAULT 'DRAFT',
        [SectionManagerId] NVARCHAR(100) NULL,
        [Status_SectionManager] NVARCHAR(20) NULL,
        [Comment_SectionManager] NVARCHAR(500) NULL,
        [ApproveInfo_SectionManager] NVARCHAR(200) NULL,
        [DepartmentManagerId] NVARCHAR(100) NULL,
        [Status_DepartmentManager] NVARCHAR(20) NULL,
        [Comment_DepartmentManager] NVARCHAR(500) NULL,
        [ApproveInfo_DepartmentManager] NVARCHAR(200) NULL,
        [ManagingDirectorId] NVARCHAR(100) NULL,
        [Status_ManagingDirector] NVARCHAR(20) NULL,
        [Comment_ManagingDirector] NVARCHAR(500) NULL,
        [ApproveInfo_ManagingDirector] NVARCHAR(200) NULL,
        [HRDAdminid] NVARCHAR(100) NULL,
        [Status_HRDAdmin] NVARCHAR(20) NULL,
        [Comment_HRDAdmin] NVARCHAR(500) NULL,
        [ApproveInfo_HRDAdmin] NVARCHAR(200) NULL,
        [HRDConfirmationid] NVARCHAR(100) NULL,
        [Status_HRDConfirmation] NVARCHAR(20) NULL,
        [Comment_HRDConfirmation] NVARCHAR(500) NULL,
        [ApproveInfo_HRDConfirmation] NVARCHAR(200) NULL,
        [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [UpdatedDate] DATETIME2(3) NULL,
        [UpdatedBy] NVARCHAR(100) NULL,
        [IsActive] BIT NULL DEFAULT 1
    );
    PRINT 'CREATED  >> Table [dbo].[TrainingRequests]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingRequests] - skip'
GO

-- 1.2 TrainingRequestEmployees
IF OBJECT_ID('dbo.TrainingRequestEmployees', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingRequestEmployees] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NULL,
        [EmployeeCode] NVARCHAR(20) NULL,
        [EmployeeName] NVARCHAR(100) NULL,
        [Position] NVARCHAR(100) NULL,
        [Department] NVARCHAR(100) NULL,
        [Level] NVARCHAR(100) NULL,
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
    PRINT 'CREATED  >> Table [dbo].[TrainingRequestEmployees]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingRequestEmployees] - skip'
GO

-- 1.3 TrainingRequestAttachments
IF OBJECT_ID('dbo.TrainingRequestAttachments', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingRequestAttachments] (
        [ID] INT PRIMARY KEY IDENTITY(1,1),
        [DocNo] NVARCHAR(20) NULL,
        [File_Name] NVARCHAR(255) NULL,
        [Modify_Date] NVARCHAR(50) NULL
    );
    PRINT 'CREATED  >> Table [dbo].[TrainingRequestAttachments]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingRequestAttachments] - skip'
GO

-- 1.4 TrainingRequest_Cost
IF OBJECT_ID('dbo.TrainingRequest_Cost', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingRequest_Cost] (
        [ID] INT PRIMARY KEY IDENTITY(1,1),
        [Department] NVARCHAR(100) NULL,
        [Year] NVARCHAR(50) NULL,
        [Cost] DECIMAL(12, 2) NULL DEFAULT 0,
        [Qhours] INT NULL DEFAULT 0,
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifyBy] NVARCHAR(200) NULL
    );
    PRINT 'CREATED  >> Table [dbo].[TrainingRequest_Cost]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingRequest_Cost] - skip'
GO

-- 1.5 RetryEmailHistory
IF OBJECT_ID('dbo.RetryEmailHistory', 'U') IS NULL
BEGIN
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
    PRINT 'CREATED  >> Table [dbo].[RetryEmailHistory]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[RetryEmailHistory] - skip'
GO

-- 1.6 EmailLogs
IF OBJECT_ID('dbo.EmailLogs', 'U') IS NULL
BEGIN
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
    PRINT 'CREATED  >> Table [dbo].[EmailLogs]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[EmailLogs] - skip'
GO

-- 1.7 ApprovalHistory
IF OBJECT_ID('dbo.ApprovalHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ApprovalHistory] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NOT NULL,
        [DocNo] NVARCHAR(20) NULL,
        [ApproverRole] NVARCHAR(50) NOT NULL,
        [ApproverEmail] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(20) NOT NULL,
        [Comment] NVARCHAR(500) NULL,
        [ActionDate] DATETIME2(3) NOT NULL DEFAULT GETDATE(),
        [PreviousStatus] NVARCHAR(50) NULL,
        [NewStatus] NVARCHAR(50) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        CONSTRAINT [FK_ApprovalHistory_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );
    PRINT 'CREATED  >> Table [dbo].[ApprovalHistory]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[ApprovalHistory] - skip'
GO

-- 1.8 TrainingHistory
IF OBJECT_ID('dbo.TrainingHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingHistory] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NOT NULL,
        [EmployeeCode] NVARCHAR(20) NULL,
        [EmployeeName] NVARCHAR(100) NULL,
        [HistoryType] NVARCHAR(20) NULL,
        [TrainingDate] DATE NULL,
        [CourseName] NVARCHAR(500) NULL,
        [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_TrainingHistory_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );
    PRINT 'CREATED  >> Table [dbo].[TrainingHistory]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingHistory] - skip'
GO

-- 1.9 TrainingParticipants
IF OBJECT_ID('dbo.TrainingParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingParticipants] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [TrainingRequestId] INT NOT NULL,
        [UserID] NVARCHAR(50) NOT NULL,
        [Prefix] NVARCHAR(50) NULL,
        [Name] NVARCHAR(50) NULL,
        [Lastname] NVARCHAR(50) NULL,
        [Level] NVARCHAR(200) NULL,
        [AddedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_TrainingParticipants_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );
    PRINT 'CREATED  >> Table [dbo].[TrainingParticipants]'
END
ELSE
    PRINT 'EXISTS   >> Table [dbo].[TrainingParticipants] - skip'
GO

PRINT ''
PRINT '========================================================'
PRINT '--- SECTION 2: Check & Add Columns for Each Table ---'
PRINT '========================================================'
PRINT ''

-- =====================================================
-- SECTION 2.1: TrainingRequests - Base Columns
-- =====================================================
PRINT '--- Table: TrainingRequests (Base Columns) ---'

-- DocNo
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [DocNo] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.DocNo NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.DocNo - skip'

-- Company
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Company')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Company] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingRequests.Company NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Company - skip'

-- TrainingType
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TrainingType')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TrainingType] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.TrainingType NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TrainingType - skip'

-- Factory
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Factory')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Factory] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.Factory NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Factory - skip'

-- CCEmail
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'CCEmail')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [CCEmail] NVARCHAR(250) NULL;
    PRINT 'ADDED    >> TrainingRequests.CCEmail NVARCHAR(250)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.CCEmail - skip'

-- Position
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Position')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Position] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.Position NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Position - skip'

-- Department
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Department')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Department] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.Department NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Department - skip'

-- EmployeeCode
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'EmployeeCode')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [EmployeeCode] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.EmployeeCode NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.EmployeeCode - skip'

-- StartDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'StartDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [StartDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.StartDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.StartDate - skip'

-- EndDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'EndDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [EndDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.EndDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.EndDate - skip'

-- SeminarTitle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'SeminarTitle')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [SeminarTitle] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.SeminarTitle NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.SeminarTitle - skip'

-- TrainingLocation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TrainingLocation')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TrainingLocation] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.TrainingLocation NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TrainingLocation - skip'

-- Instructor
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Instructor')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Instructor] NVARCHAR(150) NULL;
    PRINT 'ADDED    >> TrainingRequests.Instructor NVARCHAR(150)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Instructor - skip'

-- TotalCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TotalCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TotalCost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.TotalCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TotalCost - skip'

-- CostPerPerson
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'CostPerPerson')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [CostPerPerson] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.CostPerPerson DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.CostPerPerson - skip'

-- PerPersonTrainingHours
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'PerPersonTrainingHours')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [PerPersonTrainingHours] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.PerPersonTrainingHours INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.PerPersonTrainingHours - skip'

-- RegistrationCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'RegistrationCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [RegistrationCost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.RegistrationCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.RegistrationCost - skip'

-- InstructorFee
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'InstructorFee')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [InstructorFee] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.InstructorFee DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.InstructorFee - skip'

-- EquipmentCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'EquipmentCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [EquipmentCost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.EquipmentCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.EquipmentCost - skip'

-- FoodCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'FoodCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [FoodCost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.FoodCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.FoodCost - skip'

-- OtherCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'OtherCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [OtherCost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.OtherCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.OtherCost - skip'

-- OtherCostDescription
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'OtherCostDescription')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [OtherCostDescription] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.OtherCostDescription NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.OtherCostDescription - skip'

-- TotalPeople
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TotalPeople')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TotalPeople] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.TotalPeople INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TotalPeople - skip'

-- TrainingObjective
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TrainingObjective')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TrainingObjective] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.TrainingObjective NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TrainingObjective - skip'

-- OtherObjective
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'OtherObjective')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [OtherObjective] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.OtherObjective NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.OtherObjective - skip'

-- URLSource
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'URLSource')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [URLSource] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.URLSource NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.URLSource - skip'

-- AdditionalNotes
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'AdditionalNotes')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [AdditionalNotes] NVARCHAR(1000) NULL;
    PRINT 'ADDED    >> TrainingRequests.AdditionalNotes NVARCHAR(1000)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.AdditionalNotes - skip'

-- ExpectedOutcome
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ExpectedOutcome')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ExpectedOutcome] NVARCHAR(1000) NULL;
    PRINT 'ADDED    >> TrainingRequests.ExpectedOutcome NVARCHAR(1000)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ExpectedOutcome - skip'

-- AttachedFilePath
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'AttachedFilePath')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [AttachedFilePath] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.AttachedFilePath NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.AttachedFilePath - skip'

-- Status
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status] NVARCHAR(50) NULL DEFAULT 'DRAFT';
    PRINT 'ADDED    >> TrainingRequests.Status NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (Approval Columns) ---'

-- SectionManagerId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'SectionManagerId')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [SectionManagerId] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.SectionManagerId NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.SectionManagerId - skip'

-- Status_SectionManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_SectionManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_SectionManager] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_SectionManager NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_SectionManager - skip'

-- Comment_SectionManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_SectionManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_SectionManager] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_SectionManager NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_SectionManager - skip'

-- ApproveInfo_SectionManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_SectionManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_SectionManager] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_SectionManager NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_SectionManager - skip'

-- DepartmentManagerId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'DepartmentManagerId')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [DepartmentManagerId] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.DepartmentManagerId NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.DepartmentManagerId - skip'

-- Status_DepartmentManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_DepartmentManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_DepartmentManager] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_DepartmentManager NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_DepartmentManager - skip'

-- Comment_DepartmentManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_DepartmentManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_DepartmentManager] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_DepartmentManager NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_DepartmentManager - skip'

-- ApproveInfo_DepartmentManager
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_DepartmentManager')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_DepartmentManager] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_DepartmentManager NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_DepartmentManager - skip'

-- ManagingDirectorId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ManagingDirectorId')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ManagingDirectorId] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.ManagingDirectorId NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ManagingDirectorId - skip'

-- Status_ManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_ManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_ManagingDirector] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_ManagingDirector NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_ManagingDirector - skip'

-- Comment_ManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_ManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_ManagingDirector] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_ManagingDirector NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_ManagingDirector - skip'

-- ApproveInfo_ManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_ManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_ManagingDirector] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_ManagingDirector NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_ManagingDirector - skip'

-- HRDAdminid
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDAdminid')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRDAdminid] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRDAdminid NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRDAdminid - skip'

-- Status_HRDAdmin
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_HRDAdmin')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_HRDAdmin] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_HRDAdmin NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_HRDAdmin - skip'

-- Comment_HRDAdmin
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_HRDAdmin')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_HRDAdmin] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_HRDAdmin NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_HRDAdmin - skip'

-- ApproveInfo_HRDAdmin
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_HRDAdmin')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_HRDAdmin] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_HRDAdmin NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_HRDAdmin - skip'

-- HRDConfirmationid
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRDConfirmationid')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRDConfirmationid] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRDConfirmationid NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRDConfirmationid - skip'

-- Status_HRDConfirmation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_HRDConfirmation')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_HRDConfirmation] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_HRDConfirmation NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_HRDConfirmation - skip'

-- Comment_HRDConfirmation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_HRDConfirmation')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_HRDConfirmation] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_HRDConfirmation NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_HRDConfirmation - skip'

-- ApproveInfo_HRDConfirmation
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_HRDConfirmation')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_HRDConfirmation] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_HRDConfirmation NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_HRDConfirmation - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (Deputy Managing Director) ---'

-- DeputyManagingDirectorId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'DeputyManagingDirectorId')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [DeputyManagingDirectorId] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.DeputyManagingDirectorId NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.DeputyManagingDirectorId - skip'

-- Status_DeputyManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Status_DeputyManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Status_DeputyManagingDirector] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.Status_DeputyManagingDirector NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Status_DeputyManagingDirector - skip'

-- Comment_DeputyManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'Comment_DeputyManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [Comment_DeputyManagingDirector] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequests.Comment_DeputyManagingDirector NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.Comment_DeputyManagingDirector - skip'

-- ApproveInfo_DeputyManagingDirector
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'ApproveInfo_DeputyManagingDirector')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [ApproveInfo_DeputyManagingDirector] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.ApproveInfo_DeputyManagingDirector NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.ApproveInfo_DeputyManagingDirector - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (Audit Columns) ---'

-- CreatedDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> TrainingRequests.CreatedDate DATETIME2(3)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.CreatedDate - skip'

-- CreatedBy
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [CreatedBy] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.CreatedBy NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.CreatedBy - skip'

-- UpdatedDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'UpdatedDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [UpdatedDate] DATETIME2(3) NULL;
    PRINT 'ADDED    >> TrainingRequests.UpdatedDate DATETIME2(3)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.UpdatedDate - skip'

-- UpdatedBy
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'UpdatedBy')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [UpdatedBy] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.UpdatedBy NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.UpdatedBy - skip'

-- IsActive
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [IsActive] BIT NULL DEFAULT 1;
    PRINT 'ADDED    >> TrainingRequests.IsActive BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.IsActive - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (HRD Record Fields) ---'

-- HRD_ContactDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_ContactDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_ContactDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_ContactDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_ContactDate - skip'

-- HRD_ContactPerson
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_ContactPerson')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_ContactPerson] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_ContactPerson NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_ContactPerson - skip'

-- HRD_PaymentDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_PaymentDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_PaymentDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_PaymentDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_PaymentDate - skip'

-- HRD_PaymentMethod
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_PaymentMethod')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_PaymentMethod] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_PaymentMethod NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_PaymentMethod - skip'

-- HRD_RecorderSignature
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_RecorderSignature')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_RecorderSignature] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_RecorderSignature NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_RecorderSignature - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (HRD Section 4 Fields) ---'

-- HRD_TrainingRecord
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_TrainingRecord')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_TrainingRecord] BIT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.HRD_TrainingRecord BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_TrainingRecord - skip'

-- HRD_KnowledgeManagementDone
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_KnowledgeManagementDone')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_KnowledgeManagementDone] BIT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.HRD_KnowledgeManagementDone BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_KnowledgeManagementDone - skip'

-- HRD_CourseCertification
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_CourseCertification')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_CourseCertification] BIT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequests.HRD_CourseCertification BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_CourseCertification - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (Knowledge Management Fields) ---'

-- KM_SubmitDocument
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_SubmitDocument')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_SubmitDocument] BIT NULL;
    PRINT 'ADDED    >> TrainingRequests.KM_SubmitDocument BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.KM_SubmitDocument - skip'

-- KM_CreateReport
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReport')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReport] BIT NULL;
    PRINT 'ADDED    >> TrainingRequests.KM_CreateReport BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.KM_CreateReport - skip'

-- KM_CreateReportDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReportDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReportDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.KM_CreateReportDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.KM_CreateReportDate - skip'

-- KM_KnowledgeSharing
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharing')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharing] BIT NULL;
    PRINT 'ADDED    >> TrainingRequests.KM_KnowledgeSharing BIT'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.KM_KnowledgeSharing - skip'

-- KM_KnowledgeSharingDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharingDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharingDate] DATE NULL;
    PRINT 'ADDED    >> TrainingRequests.KM_KnowledgeSharingDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.KM_KnowledgeSharingDate - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (HRD Budget & Membership Fields) ---'

-- HRD_BudgetPlan
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetPlan')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetPlan] NVARCHAR(10) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_BudgetPlan NVARCHAR(10)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_BudgetPlan - skip'

-- HRD_BudgetUsage
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetUsage')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetUsage] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_BudgetUsage NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_BudgetUsage - skip'

-- BudgetSource
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'BudgetSource')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [BudgetSource] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.BudgetSource NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.BudgetSource - skip'

-- HRD_DepartmentBudgetRemaining
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_DepartmentBudgetRemaining')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_DepartmentBudgetRemaining] DECIMAL(12, 2) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_DepartmentBudgetRemaining DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_DepartmentBudgetRemaining - skip'

-- HRD_MembershipType
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipType')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipType] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_MembershipType NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_MembershipType - skip'

-- HRD_MembershipCost
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipCost] DECIMAL(12, 2) NULL;
    PRINT 'ADDED    >> TrainingRequests.HRD_MembershipCost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.HRD_MembershipCost - skip'

PRINT ''
PRINT '--- Table: TrainingRequests (Travel & Target Group Fields) ---'

-- TravelMethod
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TravelMethod')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TravelMethod] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequests.TravelMethod NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TravelMethod - skip'

-- TargetGroup
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'TargetGroup')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [TargetGroup] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequests.TargetGroup NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequests.TargetGroup - skip'

GO

-- =====================================================
-- SECTION 2.2: TrainingRequestEmployees - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: TrainingRequestEmployees ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [TrainingRequestId] INT NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'EmployeeCode')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [EmployeeCode] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.EmployeeCode NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.EmployeeCode - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'EmployeeName')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [EmployeeName] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.EmployeeName NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.EmployeeName - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'Position')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [Position] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.Position NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.Position - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'Department')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [Department] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.Department NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.Department - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'Level')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [Level] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.Level NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.Level - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'PreviousTrainingHours')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [PreviousTrainingHours] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.PreviousTrainingHours INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.PreviousTrainingHours - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'CurrentTrainingHours')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [CurrentTrainingHours] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.CurrentTrainingHours INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.CurrentTrainingHours - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'RemainingHours')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [RemainingHours] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.RemainingHours INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.RemainingHours - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'PreviousTrainingCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [PreviousTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.PreviousTrainingCost DECIMAL(10,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.PreviousTrainingCost - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'CurrentTrainingCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [CurrentTrainingCost] DECIMAL(10, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.CurrentTrainingCost DECIMAL(10,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.CurrentTrainingCost - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'RemainingCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [RemainingCost] DECIMAL(10, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequestEmployees.RemainingCost DECIMAL(10,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.RemainingCost - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestEmployees') AND name = 'Notes')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees] ADD [Notes] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingRequestEmployees.Notes NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestEmployees.Notes - skip'
GO

-- =====================================================
-- SECTION 2.3: TrainingRequestAttachments - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: TrainingRequestAttachments ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestAttachments') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestAttachments] ADD [DocNo] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingRequestAttachments.DocNo NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestAttachments.DocNo - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestAttachments') AND name = 'File_Name')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestAttachments] ADD [File_Name] NVARCHAR(255) NULL;
    PRINT 'ADDED    >> TrainingRequestAttachments.File_Name NVARCHAR(255)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestAttachments.File_Name - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequestAttachments') AND name = 'Modify_Date')
BEGIN
    ALTER TABLE [dbo].[TrainingRequestAttachments] ADD [Modify_Date] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingRequestAttachments.Modify_Date NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingRequestAttachments.Modify_Date - skip'
GO

-- =====================================================
-- SECTION 2.4: TrainingRequest_Cost - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: TrainingRequest_Cost ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'Department')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [Department] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequest_Cost.Department NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.Department - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'Year')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [Year] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingRequest_Cost.Year NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.Year - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'Cost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [Cost] DECIMAL(12, 2) NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequest_Cost.Cost DECIMAL(12,2)'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.Cost - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'Qhours')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [Qhours] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingRequest_Cost.Qhours INT'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.Qhours - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [CreatedBy] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingRequest_Cost.CreatedBy NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.CreatedBy - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequest_Cost') AND name = 'ModifyBy')
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost] ADD [ModifyBy] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingRequest_Cost.ModifyBy NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingRequest_Cost.ModifyBy - skip'
GO

-- =====================================================
-- SECTION 2.5: RetryEmailHistory - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: RetryEmailHistory ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [TrainingRequestId] INT NOT NULL;
    PRINT 'ADDED    >> RetryEmailHistory.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [DocNo] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> RetryEmailHistory.DocNo NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.DocNo - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'RetryBy')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [RetryBy] NVARCHAR(255) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> RetryEmailHistory.RetryBy NVARCHAR(255)'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.RetryBy - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'RetryDate')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [RetryDate] DATETIME NOT NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> RetryEmailHistory.RetryDate DATETIME'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.RetryDate - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'StatusAtRetry')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [StatusAtRetry] NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> RetryEmailHistory.StatusAtRetry NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.StatusAtRetry - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.RetryEmailHistory') AND name = 'IPAddress')
BEGIN
    ALTER TABLE [dbo].[RetryEmailHistory] ADD [IPAddress] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> RetryEmailHistory.IPAddress NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> RetryEmailHistory.IPAddress - skip'
GO

-- =====================================================
-- SECTION 2.6: EmailLogs - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: EmailLogs ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [TrainingRequestId] INT NULL;
    PRINT 'ADDED    >> EmailLogs.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> EmailLogs.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [DocNo] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> EmailLogs.DocNo NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.DocNo - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'RecipientEmail')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [RecipientEmail] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> EmailLogs.RecipientEmail NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.RecipientEmail - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'EmailType')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [EmailType] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> EmailLogs.EmailType NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.EmailType - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'Subject')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [Subject] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> EmailLogs.Subject NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.Subject - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'SentDate')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [SentDate] DATETIME2(3) NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> EmailLogs.SentDate DATETIME2(3)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.SentDate - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [Status] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> EmailLogs.Status NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.Status - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'ErrorMessage')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [ErrorMessage] NVARCHAR(1000) NULL;
    PRINT 'ADDED    >> EmailLogs.ErrorMessage NVARCHAR(1000)'
END
ELSE PRINT 'EXISTS   >> EmailLogs.ErrorMessage - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmailLogs') AND name = 'RetryCount')
BEGIN
    ALTER TABLE [dbo].[EmailLogs] ADD [RetryCount] INT NULL DEFAULT 0;
    PRINT 'ADDED    >> EmailLogs.RetryCount INT'
END
ELSE PRINT 'EXISTS   >> EmailLogs.RetryCount - skip'
GO

-- =====================================================
-- SECTION 2.7: ApprovalHistory - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: ApprovalHistory ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [TrainingRequestId] INT NOT NULL DEFAULT 0;
    PRINT 'ADDED    >> ApprovalHistory.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'DocNo')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [DocNo] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> ApprovalHistory.DocNo NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.DocNo - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'ApproverRole')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [ApproverRole] NVARCHAR(50) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> ApprovalHistory.ApproverRole NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.ApproverRole - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'ApproverEmail')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [ApproverEmail] NVARCHAR(100) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> ApprovalHistory.ApproverEmail NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.ApproverEmail - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'Action')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [Action] NVARCHAR(20) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> ApprovalHistory.Action NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.Action - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'Comment')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [Comment] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> ApprovalHistory.Comment NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.Comment - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'ActionDate')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [ActionDate] DATETIME2(3) NOT NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> ApprovalHistory.ActionDate DATETIME2(3)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.ActionDate - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'PreviousStatus')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [PreviousStatus] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> ApprovalHistory.PreviousStatus NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.PreviousStatus - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'NewStatus')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [NewStatus] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> ApprovalHistory.NewStatus NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.NewStatus - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ApprovalHistory') AND name = 'IpAddress')
BEGIN
    ALTER TABLE [dbo].[ApprovalHistory] ADD [IpAddress] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> ApprovalHistory.IpAddress NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> ApprovalHistory.IpAddress - skip'
GO

-- =====================================================
-- SECTION 2.8: TrainingHistory - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: TrainingHistory ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [TrainingRequestId] INT NOT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingHistory.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'EmployeeCode')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [EmployeeCode] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingHistory.EmployeeCode NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.EmployeeCode - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'EmployeeName')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [EmployeeName] NVARCHAR(100) NULL;
    PRINT 'ADDED    >> TrainingHistory.EmployeeName NVARCHAR(100)'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.EmployeeName - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'HistoryType')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [HistoryType] NVARCHAR(20) NULL;
    PRINT 'ADDED    >> TrainingHistory.HistoryType NVARCHAR(20)'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.HistoryType - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'TrainingDate')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [TrainingDate] DATE NULL;
    PRINT 'ADDED    >> TrainingHistory.TrainingDate DATE'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.TrainingDate - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'CourseName')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [CourseName] NVARCHAR(500) NULL;
    PRINT 'ADDED    >> TrainingHistory.CourseName NVARCHAR(500)'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.CourseName - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingHistory') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE [dbo].[TrainingHistory] ADD [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> TrainingHistory.CreatedDate DATETIME2(3)'
END
ELSE PRINT 'EXISTS   >> TrainingHistory.CreatedDate - skip'
GO

-- =====================================================
-- SECTION 2.9: TrainingParticipants - All Columns
-- =====================================================
PRINT ''
PRINT '--- Table: TrainingParticipants ---'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'TrainingRequestId')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [TrainingRequestId] INT NOT NULL DEFAULT 0;
    PRINT 'ADDED    >> TrainingParticipants.TrainingRequestId INT'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.TrainingRequestId - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'UserID')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [UserID] NVARCHAR(50) NOT NULL DEFAULT '';
    PRINT 'ADDED    >> TrainingParticipants.UserID NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.UserID - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'Prefix')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [Prefix] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingParticipants.Prefix NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.Prefix - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'Name')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [Name] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingParticipants.Name NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.Name - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'Lastname')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [Lastname] NVARCHAR(50) NULL;
    PRINT 'ADDED    >> TrainingParticipants.Lastname NVARCHAR(50)'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.Lastname - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'Level')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [Level] NVARCHAR(200) NULL;
    PRINT 'ADDED    >> TrainingParticipants.Level NVARCHAR(200)'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.Level - skip'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingParticipants') AND name = 'AddedDate')
BEGIN
    ALTER TABLE [dbo].[TrainingParticipants] ADD [AddedDate] DATETIME NOT NULL DEFAULT GETDATE();
    PRINT 'ADDED    >> TrainingParticipants.AddedDate DATETIME'
END
ELSE PRINT 'EXISTS   >> TrainingParticipants.AddedDate - skip'
GO

-- =====================================================
-- SECTION 3: Verification Summary
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '--- SECTION 3: Verification Summary ---'
PRINT '========================================================'
PRINT ''

-- Show column count per table
SELECT
    t.name AS [Table_Name],
    COUNT(c.name) AS [Column_Count]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.name IN (
    'TrainingRequests',
    'TrainingRequestEmployees',
    'TrainingRequestAttachments',
    'TrainingRequest_Cost',
    'RetryEmailHistory',
    'EmailLogs',
    'ApprovalHistory',
    'TrainingHistory',
    'TrainingParticipants'
)
GROUP BY t.name
ORDER BY t.name;

PRINT ''
PRINT '========================================================'
PRINT '  Script Completed Successfully!'
PRINT '  End Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
PRINT ''
PRINT 'Note:'
PRINT '- Script นี้สามารถ run ซ้ำได้ (Idempotent)'
PRINT '- ถ้า Column มีอยู่แล้วจะข้ามไปโดยอัตโนมัติ'
PRINT '- ถ้า Table ไม่มีจะสร้างใหม่พร้อม FK Constraints'
PRINT '- ข้อมูลเดิมจะไม่ถูกกระทบ'
PRINT ''
GO
