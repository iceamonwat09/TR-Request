-- =====================================================
-- Add HRD Section 4 Fields to TrainingRequests Table
-- Purpose: เพิ่มฟิลด์สำหรับ "ส่วนที่ 4 การดำเนินงานหลังอนุมัติ"
--          - บันทึกประวัติฝึกอบรม Training Record
--          - การจัดการความรู้ (KM)
--          - การยื่นขอรับรองหลักสูตร
-- Date: 2026-01-31
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '======================================='
PRINT 'Starting: Add HRD Section 4 Fields'
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
-- เพิ่ม 3 Columns ใหม่สำหรับ HRD Section 4
-- =====================================================
PRINT '--- Adding HRD Section 4 Fields ---'

-- HRD_TrainingRecord (Checkbox: บันทึกประวัติฝึกอบรม Training Record)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_TrainingRecord')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_TrainingRecord] BIT NULL DEFAULT 0;
    PRINT '✅ Added: HRD_TrainingRecord (BIT) - บันทึกประวัติฝึกอบรม'
END
ELSE PRINT '⚠️  Column HRD_TrainingRecord already exists, skipping...'

-- HRD_KnowledgeManagementDone (Checkbox: การจัดการความรู้ KM)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_KnowledgeManagementDone')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_KnowledgeManagementDone] BIT NULL DEFAULT 0;
    PRINT '✅ Added: HRD_KnowledgeManagementDone (BIT) - การจัดการความรู้'
END
ELSE PRINT '⚠️  Column HRD_KnowledgeManagementDone already exists, skipping...'

-- HRD_CourseCertification (Checkbox: การยื่นขอรับรองหลักสูตร)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_CourseCertification')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests] ADD [HRD_CourseCertification] BIT NULL DEFAULT 0;
    PRINT '✅ Added: HRD_CourseCertification (BIT) - การยื่นขอรับรองหลักสูตร'
END
ELSE PRINT '⚠️  Column HRD_CourseCertification already exists, skipping...'

PRINT ''
PRINT '======================================='
PRINT '✅ Migration Completed Successfully!'
PRINT '======================================='
PRINT ''
PRINT 'Summary:'
PRINT '- Added 3 new columns for HRD Section 4'
PRINT '  1. HRD_TrainingRecord (BIT) - บันทึกประวัติฝึกอบรม Training Record'
PRINT '  2. HRD_KnowledgeManagementDone (BIT) - การจัดการความรู้ (KM)'
PRINT '  3. HRD_CourseCertification (BIT) - การยื่นขอรับรองหลักสูตร'
PRINT '- All columns are NULLABLE with DEFAULT 0'
PRINT '- Existing data is not affected'
PRINT ''
PRINT 'Note: HRD_PaymentMethod column already supports "Check", "Cash"'
PRINT '      Now it can also store "Transfer" (โอนเงิน) - no schema change needed'
PRINT ''

GO
