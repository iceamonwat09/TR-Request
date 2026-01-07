-- =====================================================
-- Add HRD Record Fields to TrainingRequests Table
-- Purpose: เพิ่มฟิลด์สำหรับ HRD บันทึกข้อมูล
-- Date: 2026-01-07
-- Author: Claude AI
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '======================================='
PRINT 'Starting: Add HRD Record Fields'
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

-- ตรวจสอบและเพิ่มคอลัมน์ HRD_ContactDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_ContactDate')
BEGIN
    PRINT 'Adding column: HRD_ContactDate...'
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [HRD_ContactDate] DATETIME NULL;
    PRINT '✅ Added: HRD_ContactDate'
END
ELSE
BEGIN
    PRINT '⚠️  Column HRD_ContactDate already exists, skipping...'
END

-- ตรวจสอบและเพิ่มคอลัมน์ HRD_ContactPerson
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_ContactPerson')
BEGIN
    PRINT 'Adding column: HRD_ContactPerson...'
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [HRD_ContactPerson] NVARCHAR(100) NULL;
    PRINT '✅ Added: HRD_ContactPerson'
END
ELSE
BEGIN
    PRINT '⚠️  Column HRD_ContactPerson already exists, skipping...'
END

-- ตรวจสอบและเพิ่มคอลัมน์ HRD_PaymentDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_PaymentDate')
BEGIN
    PRINT 'Adding column: HRD_PaymentDate...'
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [HRD_PaymentDate] DATETIME NULL;
    PRINT '✅ Added: HRD_PaymentDate'
END
ELSE
BEGIN
    PRINT '⚠️  Column HRD_PaymentDate already exists, skipping...'
END

-- ตรวจสอบและเพิ่มคอลัมน์ HRD_PaymentMethod
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_PaymentMethod')
BEGIN
    PRINT 'Adding column: HRD_PaymentMethod...'
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [HRD_PaymentMethod] NVARCHAR(20) NULL;
    PRINT '✅ Added: HRD_PaymentMethod'
END
ELSE
BEGIN
    PRINT '⚠️  Column HRD_PaymentMethod already exists, skipping...'
END

-- ตรวจสอบและเพิ่มคอลัมน์ HRD_RecorderSignature
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TrainingRequests') AND name = 'HRD_RecorderSignature')
BEGIN
    PRINT 'Adding column: HRD_RecorderSignature...'
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [HRD_RecorderSignature] NVARCHAR(100) NULL;
    PRINT '✅ Added: HRD_RecorderSignature'
END
ELSE
BEGIN
    PRINT '⚠️  Column HRD_RecorderSignature already exists, skipping...'
END

PRINT ''
PRINT '======================================='
PRINT '✅ Migration Completed Successfully!'
PRINT '======================================='
PRINT ''
PRINT 'Summary:'
PRINT '- Added 5 new columns for HRD Record Section'
PRINT '- All columns are NULLABLE (optional)'
PRINT '- Existing data is not affected'
PRINT ''
PRINT 'Next Steps:'
PRINT '1. Update Models/TrainingRequest.cs'
PRINT '2. Update Controllers/TrainingRequestController.cs'
PRINT '3. Update Views/TrainingRequest/Edit.cshtml'
PRINT '4. Update Services/PdfReportService.cs'
PRINT ''

GO
