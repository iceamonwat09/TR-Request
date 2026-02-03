-- =====================================================
-- Add Knowledge Management & HRD Budget Fields to TrainingRequests Table
-- Purpose: เพิ่มฟิลด์ Knowledge Management (ส่วนที่ 1)
--          และฟิลด์ HRD งบประมาณ/สมาชิก (ส่วนที่ 2)
-- Date: 2026-01-28
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '======================================='
PRINT 'Starting: Add KM & HRD Budget Fields'
PRINT '======================================='
PRINT ''

-- ตรวจสอบว่าตารางมีอยู่หรือไม่
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NULL
BEGIN
    PRINT '❌ ERROR: Table [dbo].[TrainingRequests] does not exist!'
    PRINT 'Please run 01_CreateTable_TrainingRequests.sql first.'
    RETURN
END

PRINT '✅ Table [dbo].[TrainingRequests] exists'
PRINT ''

-- =====================================================
-- ส่วนที่ 1: Knowledge Management Fields
-- =====================================================
PRINT '--- ส่วนที่ 1: Knowledge Management ---'

-- KM_SubmitDocument (Checkbox: นำส่งเอกสาร)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_SubmitDocument')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_SubmitDocument] BIT NULL;
    PRINT '✅ Added: KM_SubmitDocument (BIT)'
END
ELSE PRINT '⚠️  Column KM_SubmitDocument already exists, skipping...'

-- KM_CreateReport (Checkbox: จัดทำรายงาน/PPT)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReport')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReport] BIT NULL;
    PRINT '✅ Added: KM_CreateReport (BIT)'
END
ELSE PRINT '⚠️  Column KM_CreateReport already exists, skipping...'

-- KM_CreateReportDate (วันที่ดำเนินการ - รายงาน/PPT)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReportDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReportDate] DATE NULL;
    PRINT '✅ Added: KM_CreateReportDate (DATE)'
END
ELSE PRINT '⚠️  Column KM_CreateReportDate already exists, skipping...'

-- KM_KnowledgeSharing (Checkbox: ถ่ายทอดความรู้)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharing')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharing] BIT NULL;
    PRINT '✅ Added: KM_KnowledgeSharing (BIT)'
END
ELSE PRINT '⚠️  Column KM_KnowledgeSharing already exists, skipping...'

-- KM_KnowledgeSharingDate (วันที่ดำเนินการ - ถ่ายทอดความรู้)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharingDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharingDate] DATE NULL;
    PRINT '✅ Added: KM_KnowledgeSharingDate (DATE)'
END
ELSE PRINT '⚠️  Column KM_KnowledgeSharingDate already exists, skipping...'

PRINT ''

-- =====================================================
-- ส่วนที่ 2: HRD Budget & Membership Fields
-- =====================================================
PRINT '--- ส่วนที่ 2: HRD Budget & Membership ---'

-- HRD_BudgetPlan (Plan/Unplan)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetPlan')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetPlan] NVARCHAR(10) NULL;
    PRINT '✅ Added: HRD_BudgetPlan (NVARCHAR(10))'
END
ELSE PRINT '⚠️  Column HRD_BudgetPlan already exists, skipping...'

-- HRD_BudgetUsage (TYP/Department)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetUsage')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetUsage] NVARCHAR(20) NULL;
    PRINT '✅ Added: HRD_BudgetUsage (NVARCHAR(20))'
END
ELSE PRINT '⚠️  Column HRD_BudgetUsage already exists, skipping...'

-- HRD_DepartmentBudgetRemaining (ยอดเงินคงเหลือ)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_DepartmentBudgetRemaining')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_DepartmentBudgetRemaining] DECIMAL(12, 2) NULL;
    PRINT '✅ Added: HRD_DepartmentBudgetRemaining (DECIMAL(12,2))'
END
ELSE PRINT '⚠️  Column HRD_DepartmentBudgetRemaining already exists, skipping...'

-- HRD_MembershipType (Member/NonMember)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipType')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipType] NVARCHAR(20) NULL;
    PRINT '✅ Added: HRD_MembershipType (NVARCHAR(20))'
END
ELSE PRINT '⚠️  Column HRD_MembershipType already exists, skipping...'

-- HRD_MembershipCost (จำนวนเงินสมาชิก)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipCost] DECIMAL(12, 2) NULL;
    PRINT '✅ Added: HRD_MembershipCost (DECIMAL(12,2))'
END
ELSE PRINT '⚠️  Column HRD_MembershipCost already exists, skipping...'

PRINT ''
PRINT '======================================='
PRINT '✅ Migration Completed Successfully!'
PRINT '======================================='
PRINT ''
PRINT 'Summary:'
PRINT '- ส่วนที่ 1: Added 5 KM columns (KM_SubmitDocument, KM_CreateReport, KM_CreateReportDate, KM_KnowledgeSharing, KM_KnowledgeSharingDate)'
PRINT '- ส่วนที่ 2: Added 5 HRD Budget columns (HRD_BudgetPlan, HRD_BudgetUsage, HRD_DepartmentBudgetRemaining, HRD_MembershipType, HRD_MembershipCost)'
PRINT '- All columns are NULLABLE (optional)'
PRINT '- Existing data is not affected'
PRINT ''

GO
