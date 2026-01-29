-- =====================================================
-- COMPLETE MIGRATION SCRIPT
-- Knowledge Management & HRD Admin Section
-- =====================================================
-- Purpose:
--   1. เพิ่ม 10 columns ใน TrainingRequests (KM + HRD Budget)
--   2. สร้างตาราง TrainingHistory (ประวัติการอบรม)
--   3. สร้าง Indexes
--
-- Date: 2026-01-29
-- Version: 1.0
--
-- Note: Script นี้สามารถรันซ้ำได้โดยไม่เกิดปัญหา (Idempotent)
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON;

PRINT ''
PRINT '╔══════════════════════════════════════════════════════════════╗'
PRINT '║     MIGRATION: Knowledge Management & HRD Admin Section      ║'
PRINT '╠══════════════════════════════════════════════════════════════╣'
PRINT '║  1. TrainingRequests: +10 columns (KM + HRD Budget)          ║'
PRINT '║  2. TrainingHistory: New table (ประวัติการอบรม)                ║'
PRINT '╚══════════════════════════════════════════════════════════════╝'
PRINT ''

-- =====================================================
-- PRE-CHECK: ตรวจสอบว่าตาราง TrainingRequests มีอยู่หรือไม่
-- =====================================================
IF OBJECT_ID('dbo.TrainingRequests', 'U') IS NULL
BEGIN
    PRINT '❌ ERROR: Table [dbo].[TrainingRequests] does not exist!'
    PRINT '   Please create TrainingRequests table first.'
    PRINT ''
    RAISERROR('TrainingRequests table not found. Migration aborted.', 16, 1)
    RETURN
END

PRINT '✅ PRE-CHECK: Table [dbo].[TrainingRequests] exists'
PRINT ''

-- =====================================================
-- PART 1: Knowledge Management Fields (5 columns)
-- =====================================================
PRINT '┌──────────────────────────────────────────────────────────────┐'
PRINT '│ PART 1: Knowledge Management Fields                          │'
PRINT '│ (แสดงเฉพาะ TrainingType = Public)                             │'
PRINT '└──────────────────────────────────────────────────────────────┘'

-- 1.1 KM_SubmitDocument (Checkbox: นำส่งเอกสาร)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_SubmitDocument')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_SubmitDocument] BIT NULL;
    PRINT '  ✅ Added: KM_SubmitDocument (BIT) - นำส่งเอกสาร'
END
ELSE
    PRINT '  ⚠️  Skip: KM_SubmitDocument already exists'

-- 1.2 KM_CreateReport (Checkbox: จัดทำรายงาน/PPT)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReport')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReport] BIT NULL;
    PRINT '  ✅ Added: KM_CreateReport (BIT) - จัดทำรายงาน/PPT'
END
ELSE
    PRINT '  ⚠️  Skip: KM_CreateReport already exists'

-- 1.3 KM_CreateReportDate (วันที่ดำเนินการ - รายงาน/PPT)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_CreateReportDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_CreateReportDate] DATE NULL;
    PRINT '  ✅ Added: KM_CreateReportDate (DATE) - วันที่รายงาน'
END
ELSE
    PRINT '  ⚠️  Skip: KM_CreateReportDate already exists'

-- 1.4 KM_KnowledgeSharing (Checkbox: ถ่ายทอดความรู้)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharing')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharing] BIT NULL;
    PRINT '  ✅ Added: KM_KnowledgeSharing (BIT) - ถ่ายทอดความรู้'
END
ELSE
    PRINT '  ⚠️  Skip: KM_KnowledgeSharing already exists'

-- 1.5 KM_KnowledgeSharingDate (วันที่ดำเนินการ - ถ่ายทอดความรู้)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'KM_KnowledgeSharingDate')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [KM_KnowledgeSharingDate] DATE NULL;
    PRINT '  ✅ Added: KM_KnowledgeSharingDate (DATE) - วันที่ถ่ายทอด'
END
ELSE
    PRINT '  ⚠️  Skip: KM_KnowledgeSharingDate already exists'

PRINT ''

-- =====================================================
-- PART 2: HRD Budget & Membership Fields (5 columns)
-- =====================================================
PRINT '┌──────────────────────────────────────────────────────────────┐'
PRINT '│ PART 2: HRD Budget & Membership Fields                       │'
PRINT '│ (แก้ไขได้เฉพาะ HRD Admin)                                      │'
PRINT '└──────────────────────────────────────────────────────────────┘'

-- 2.1 HRD_BudgetPlan (Plan/Unplan)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetPlan')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetPlan] NVARCHAR(10) NULL;
    PRINT '  ✅ Added: HRD_BudgetPlan (NVARCHAR(10)) - การวางแผนงบประมาณ'
END
ELSE
    PRINT '  ⚠️  Skip: HRD_BudgetPlan already exists'

-- 2.2 HRD_BudgetUsage (TYP/Department)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_BudgetUsage')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_BudgetUsage] NVARCHAR(20) NULL;
    PRINT '  ✅ Added: HRD_BudgetUsage (NVARCHAR(20)) - การใช้งบประมาณ'
END
ELSE
    PRINT '  ⚠️  Skip: HRD_BudgetUsage already exists'

-- 2.3 HRD_DepartmentBudgetRemaining (ยอดเงินคงเหลือ)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_DepartmentBudgetRemaining')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_DepartmentBudgetRemaining] DECIMAL(12, 2) NULL;
    PRINT '  ✅ Added: HRD_DepartmentBudgetRemaining (DECIMAL(12,2)) - งบคงเหลือ'
END
ELSE
    PRINT '  ⚠️  Skip: HRD_DepartmentBudgetRemaining already exists'

-- 2.4 HRD_MembershipType (Member/NonMember)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipType')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipType] NVARCHAR(20) NULL;
    PRINT '  ✅ Added: HRD_MembershipType (NVARCHAR(20)) - การเป็นสมาชิก'
END
ELSE
    PRINT '  ⚠️  Skip: HRD_MembershipType already exists'

-- 2.5 HRD_MembershipCost (จำนวนเงินสมาชิก)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_MembershipCost')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_MembershipCost] DECIMAL(12, 2) NULL;
    PRINT '  ✅ Added: HRD_MembershipCost (DECIMAL(12,2)) - ค่าสมาชิก'
END
ELSE
    PRINT '  ⚠️  Skip: HRD_MembershipCost already exists'

PRINT ''

-- =====================================================
-- PART 3: Create TrainingHistory Table
-- =====================================================
PRINT '┌──────────────────────────────────────────────────────────────┐'
PRINT '│ PART 3: Create TrainingHistory Table                         │'
PRINT '│ (ตารางประวัติการอบรม - HRD Section)                            │'
PRINT '└──────────────────────────────────────────────────────────────┘'

IF OBJECT_ID('dbo.TrainingHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingHistory] (
        -- Primary Key
        [Id] INT PRIMARY KEY IDENTITY(1,1),

        -- Foreign Key to TrainingRequests
        [TrainingRequestId] INT NOT NULL,

        -- Employee Information
        [EmployeeCode] NVARCHAR(20) NULL,
        [EmployeeName] NVARCHAR(100) NULL,

        -- History Type: Never(ไม่เคย), Ever(เคย), Similar(ใกล้เคียง)
        [HistoryType] NVARCHAR(20) NULL,

        -- Training Details
        [TrainingDate] DATE NULL,
        [CourseName] NVARCHAR(500) NULL,

        -- Audit
        [CreatedDate] DATETIME2(3) NULL DEFAULT GETDATE(),

        -- Foreign Key Constraint with CASCADE DELETE
        CONSTRAINT [FK_TrainingHistory_TrainingRequests]
            FOREIGN KEY ([TrainingRequestId])
            REFERENCES [dbo].[TrainingRequests]([Id])
            ON DELETE CASCADE
    );

    PRINT '  ✅ Created: Table [dbo].[TrainingHistory]'
    PRINT '     - Id (PK, IDENTITY)'
    PRINT '     - TrainingRequestId (FK -> TrainingRequests, CASCADE DELETE)'
    PRINT '     - EmployeeCode, EmployeeName'
    PRINT '     - HistoryType (Never/Ever/Similar)'
    PRINT '     - TrainingDate, CourseName'
    PRINT '     - CreatedDate'
END
ELSE
    PRINT '  ⚠️  Skip: Table [dbo].[TrainingHistory] already exists'

PRINT ''

-- =====================================================
-- PART 4: Create Indexes
-- =====================================================
PRINT '┌──────────────────────────────────────────────────────────────┐'
PRINT '│ PART 4: Create Indexes                                       │'
PRINT '└──────────────────────────────────────────────────────────────┘'

-- Index on TrainingRequestId for fast lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_TrainingRequestId' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
BEGIN
    CREATE INDEX [IX_TrainingHistory_TrainingRequestId]
        ON [dbo].[TrainingHistory]([TrainingRequestId]);
    PRINT '  ✅ Created: IX_TrainingHistory_TrainingRequestId'
END
ELSE
    PRINT '  ⚠️  Skip: IX_TrainingHistory_TrainingRequestId already exists'

-- Index on EmployeeCode for employee search
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingHistory_EmployeeCode' AND object_id = OBJECT_ID('dbo.TrainingHistory'))
BEGIN
    CREATE INDEX [IX_TrainingHistory_EmployeeCode]
        ON [dbo].[TrainingHistory]([EmployeeCode]);
    PRINT '  ✅ Created: IX_TrainingHistory_EmployeeCode'
END
ELSE
    PRINT '  ⚠️  Skip: IX_TrainingHistory_EmployeeCode already exists'

PRINT ''

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT '╔══════════════════════════════════════════════════════════════╗'
PRINT '║                    MIGRATION COMPLETED                       ║'
PRINT '╠══════════════════════════════════════════════════════════════╣'
PRINT '║                                                              ║'
PRINT '║  TrainingRequests Table:                                     ║'
PRINT '║  ├─ KM_SubmitDocument (BIT)         - นำส่งเอกสาร             ║'
PRINT '║  ├─ KM_CreateReport (BIT)           - จัดทำรายงาน/PPT         ║'
PRINT '║  ├─ KM_CreateReportDate (DATE)      - วันที่รายงาน            ║'
PRINT '║  ├─ KM_KnowledgeSharing (BIT)       - ถ่ายทอดความรู้          ║'
PRINT '║  ├─ KM_KnowledgeSharingDate (DATE)  - วันที่ถ่ายทอด           ║'
PRINT '║  ├─ HRD_BudgetPlan (NVARCHAR)       - Plan/Unplan            ║'
PRINT '║  ├─ HRD_BudgetUsage (NVARCHAR)      - TYP/Department         ║'
PRINT '║  ├─ HRD_DepartmentBudgetRemaining   - งบคงเหลือ              ║'
PRINT '║  ├─ HRD_MembershipType (NVARCHAR)   - Member/NonMember       ║'
PRINT '║  └─ HRD_MembershipCost (DECIMAL)    - ค่าสมาชิก              ║'
PRINT '║                                                              ║'
PRINT '║  TrainingHistory Table (NEW):                                ║'
PRINT '║  ├─ Id (PK)                                                  ║'
PRINT '║  ├─ TrainingRequestId (FK)                                   ║'
PRINT '║  ├─ EmployeeCode, EmployeeName                               ║'
PRINT '║  ├─ HistoryType (Never/Ever/Similar)                         ║'
PRINT '║  ├─ TrainingDate, CourseName                                 ║'
PRINT '║  └─ CreatedDate                                              ║'
PRINT '║                                                              ║'
PRINT '║  Indexes:                                                    ║'
PRINT '║  ├─ IX_TrainingHistory_TrainingRequestId                     ║'
PRINT '║  └─ IX_TrainingHistory_EmployeeCode                          ║'
PRINT '║                                                              ║'
PRINT '╚══════════════════════════════════════════════════════════════╝'
PRINT ''

GO

-- =====================================================
-- VERIFICATION QUERY (Optional)
-- =====================================================
-- Uncomment to verify columns were added:
/*
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.TrainingRequests')
AND c.name IN (
    'KM_SubmitDocument', 'KM_CreateReport', 'KM_CreateReportDate',
    'KM_KnowledgeSharing', 'KM_KnowledgeSharingDate',
    'HRD_BudgetPlan', 'HRD_BudgetUsage', 'HRD_DepartmentBudgetRemaining',
    'HRD_MembershipType', 'HRD_MembershipCost'
)
ORDER BY c.name;

SELECT
    c.name AS ColumnName,
    t.name AS DataType
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.TrainingHistory')
ORDER BY c.column_id;
*/
