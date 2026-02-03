-- =====================================================
-- COMPLETE MIGRATION SCRIPT
-- Knowledge Management & HRD Admin Section
-- =====================================================
-- Purpose:
--   1. เพิ่ม 10 columns ใน TrainingRequests (KM + HRD Budget)
--   2. เพิ่ม 3 columns สำหรับ HRD Section 4 (Training Record, KM Done, Course Cert)
--   3. สร้างตาราง TrainingHistory (ประวัติการอบรม)
--   4. สร้าง Indexes
--
-- Date: 2026-01-31
-- Version: 2.0 (Added HRD Section 4 Fields)
--
-- Columns Added (Total: 13 columns):
--   - Part 1: Knowledge Management (5 columns)
--   - Part 2: HRD Budget & Membership (5 columns)
--   - Part 3: HRD Section 4 - การดำเนินงานหลังอนุมัติ (3 columns)
--
-- Note: Script นี้สามารถรันซ้ำได้โดยไม่เกิดปัญหา (Idempotent)
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON;

PRINT ''
PRINT '========================================================================'
PRINT '     MIGRATION: Knowledge Management & HRD Admin Section (v2.0)'
PRINT '========================================================================'
PRINT '  1. TrainingRequests: +13 columns (KM + HRD Budget + HRD Section 4)'
PRINT '  2. TrainingHistory: New table'
PRINT '========================================================================'
PRINT ''

-- =====================================================
-- PRE-CHECK: ตรวจสอบว่าตาราง TrainingRequests มีอยู่หรือไม่
-- =====================================================
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NULL
BEGIN
    PRINT '>>> ERROR: Table [dbo].[TrainingRequests] does not exist!'
    PRINT '    Please create TrainingRequests table first.'
    PRINT ''
END
ELSE
BEGIN
    PRINT '>>> PRE-CHECK: Table [dbo].[TrainingRequests] exists'
    PRINT ''

    -- =====================================================
    -- PART 1: Knowledge Management Fields (5 columns)
    -- =====================================================
    PRINT '------------------------------------------------------------------------'
    PRINT ' PART 1: Knowledge Management Fields (5 columns)'
    PRINT '------------------------------------------------------------------------'

    -- 1.1 KM_SubmitDocument
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_SubmitDocument')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [KM_SubmitDocument] BIT NULL;
        PRINT '  [OK] Added: KM_SubmitDocument (BIT)'
    END
    ELSE
        PRINT '  [SKIP] KM_SubmitDocument already exists'

    -- 1.2 KM_CreateReport
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReport')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReport] BIT NULL;
        PRINT '  [OK] Added: KM_CreateReport (BIT)'
    END
    ELSE
        PRINT '  [SKIP] KM_CreateReport already exists'

    -- 1.3 KM_CreateReportDate
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReportDate')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReportDate] DATE NULL;
        PRINT '  [OK] Added: KM_CreateReportDate (DATE)'
    END
    ELSE
        PRINT '  [SKIP] KM_CreateReportDate already exists'

    -- 1.4 KM_KnowledgeSharing
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharing')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharing] BIT NULL;
        PRINT '  [OK] Added: KM_KnowledgeSharing (BIT)'
    END
    ELSE
        PRINT '  [SKIP] KM_KnowledgeSharing already exists'

    -- 1.5 KM_KnowledgeSharingDate
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharingDate')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharingDate] DATE NULL;
        PRINT '  [OK] Added: KM_KnowledgeSharingDate (DATE)'
    END
    ELSE
        PRINT '  [SKIP] KM_KnowledgeSharingDate already exists'

    PRINT ''

    -- =====================================================
    -- PART 2: HRD Budget & Membership Fields (5 columns)
    -- =====================================================
    PRINT '------------------------------------------------------------------------'
    PRINT ' PART 2: HRD Budget & Membership Fields (5 columns)'
    PRINT '------------------------------------------------------------------------'

    -- 2.1 HRD_BudgetPlan
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetPlan')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetPlan] NVARCHAR(10) NULL;
        PRINT '  [OK] Added: HRD_BudgetPlan (NVARCHAR(10))'
    END
    ELSE
        PRINT '  [SKIP] HRD_BudgetPlan already exists'

    -- 2.2 HRD_BudgetUsage
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetUsage')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetUsage] NVARCHAR(20) NULL;
        PRINT '  [OK] Added: HRD_BudgetUsage (NVARCHAR(20))'
    END
    ELSE
        PRINT '  [SKIP] HRD_BudgetUsage already exists'

    -- 2.3 HRD_DepartmentBudgetRemaining
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_DepartmentBudgetRemaining')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_DepartmentBudgetRemaining] DECIMAL(12, 2) NULL;
        PRINT '  [OK] Added: HRD_DepartmentBudgetRemaining (DECIMAL(12,2))'
    END
    ELSE
        PRINT '  [SKIP] HRD_DepartmentBudgetRemaining already exists'

    -- 2.4 HRD_MembershipType
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipType')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipType] NVARCHAR(20) NULL;
        PRINT '  [OK] Added: HRD_MembershipType (NVARCHAR(20))'
    END
    ELSE
        PRINT '  [SKIP] HRD_MembershipType already exists'

    -- 2.5 HRD_MembershipCost
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipCost')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipCost] DECIMAL(12, 2) NULL;
        PRINT '  [OK] Added: HRD_MembershipCost (DECIMAL(12,2))'
    END
    ELSE
        PRINT '  [SKIP] HRD_MembershipCost already exists'

    PRINT ''

    -- =====================================================
    -- PART 3: HRD Section 4 - การดำเนินงานหลังอนุมัติ (3 columns)
    -- =====================================================
    PRINT '------------------------------------------------------------------------'
    PRINT ' PART 3: HRD Section 4 - การดำเนินงานหลังอนุมัติ (3 columns)'
    PRINT '------------------------------------------------------------------------'

    -- 3.1 HRD_TrainingRecord (Checkbox: บันทึกประวัติฝึกอบรม Training Record)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_TrainingRecord')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_TrainingRecord] BIT NULL DEFAULT 0;
        PRINT '  [OK] Added: HRD_TrainingRecord (BIT) - บันทึกประวัติฝึกอบรม'
    END
    ELSE
        PRINT '  [SKIP] HRD_TrainingRecord already exists'

    -- 3.2 HRD_KnowledgeManagementDone (Checkbox: การจัดการความรู้ KM)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_KnowledgeManagementDone')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_KnowledgeManagementDone] BIT NULL DEFAULT 0;
        PRINT '  [OK] Added: HRD_KnowledgeManagementDone (BIT) - การจัดการความรู้'
    END
    ELSE
        PRINT '  [SKIP] HRD_KnowledgeManagementDone already exists'

    -- 3.3 HRD_CourseCertification (Checkbox: การยื่นขอรับรองหลักสูตร)
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_CourseCertification')
    BEGIN
        ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_CourseCertification] BIT NULL DEFAULT 0;
        PRINT '  [OK] Added: HRD_CourseCertification (BIT) - การยื่นขอรับรองหลักสูตร'
    END
    ELSE
        PRINT '  [SKIP] HRD_CourseCertification already exists'

    PRINT ''

    -- =====================================================
    -- PART 4: Create TrainingHistory Table
    -- =====================================================
    PRINT '------------------------------------------------------------------------'
    PRINT ' PART 4: Create TrainingHistory Table'
    PRINT '------------------------------------------------------------------------'

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
        PRINT '  [OK] Created: Table [dbo].[TrainingHistory]'
    END
    ELSE
        PRINT '  [SKIP] Table [dbo].[TrainingHistory] already exists'

    PRINT ''

    -- =====================================================
    -- PART 5: Create Indexes
    -- =====================================================
    PRINT '------------------------------------------------------------------------'
    PRINT ' PART 5: Create Indexes'
    PRINT '------------------------------------------------------------------------'

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_TrainingRequestId' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
    BEGIN
        CREATE INDEX [IX_TrainingHistory_TrainingRequestId]
            ON [dbo].[TrainingHistory]([TrainingRequestId]);
        PRINT '  [OK] Created: IX_TrainingHistory_TrainingRequestId'
    END
    ELSE
        PRINT '  [SKIP] IX_TrainingHistory_TrainingRequestId already exists'

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_EmployeeCode' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
    BEGIN
        CREATE INDEX [IX_TrainingHistory_EmployeeCode]
            ON [dbo].[TrainingHistory]([EmployeeCode]);
        PRINT '  [OK] Created: IX_TrainingHistory_EmployeeCode'
    END
    ELSE
        PRINT '  [SKIP] IX_TrainingHistory_EmployeeCode already exists'

    PRINT ''
    PRINT '========================================================================'
    PRINT '                      MIGRATION COMPLETED'
    PRINT '========================================================================'
    PRINT ''
    PRINT ' TrainingRequests Table (+13 columns):'
    PRINT ''
    PRINT '   --- Part 1: Knowledge Management ---'
    PRINT '   - KM_SubmitDocument (BIT)'
    PRINT '   - KM_CreateReport (BIT)'
    PRINT '   - KM_CreateReportDate (DATE)'
    PRINT '   - KM_KnowledgeSharing (BIT)'
    PRINT '   - KM_KnowledgeSharingDate (DATE)'
    PRINT ''
    PRINT '   --- Part 2: HRD Budget & Membership ---'
    PRINT '   - HRD_BudgetPlan (NVARCHAR(10))'
    PRINT '   - HRD_BudgetUsage (NVARCHAR(20))'
    PRINT '   - HRD_DepartmentBudgetRemaining (DECIMAL(12,2))'
    PRINT '   - HRD_MembershipType (NVARCHAR(20))'
    PRINT '   - HRD_MembershipCost (DECIMAL(12,2))'
    PRINT ''
    PRINT '   --- Part 3: HRD Section 4 (การดำเนินงานหลังอนุมัติ) ---'
    PRINT '   - HRD_TrainingRecord (BIT) - บันทึกประวัติฝึกอบรม Training Record'
    PRINT '   - HRD_KnowledgeManagementDone (BIT) - การจัดการความรู้ (KM)'
    PRINT '   - HRD_CourseCertification (BIT) - การยื่นขอรับรองหลักสูตร'
    PRINT ''
    PRINT ' TrainingHistory Table (NEW):'
    PRINT '   - Id (PK, IDENTITY)'
    PRINT '   - TrainingRequestId (FK -> TrainingRequests, CASCADE DELETE)'
    PRINT '   - EmployeeCode, EmployeeName'
    PRINT '   - HistoryType (Never/Ever/Similar)'
    PRINT '   - TrainingDate, CourseName'
    PRINT '   - CreatedDate'
    PRINT ''
    PRINT ' Note: HRD_PaymentMethod supports "Check", "Transfer", "Cash"'
    PRINT ''
    PRINT '========================================================================'

END
GO
