-- ============================================================
-- Migration: Add BudgetSource column to TrainingRequests
-- Purpose: เก็บแหล่งงบประมาณ ('TYP' = งบกลาง, 'Department' = งบต้นสังกัด)
-- Date: 2026-02-08
-- ============================================================

-- 1. เพิ่มคอลัมน์ BudgetSource ใน TrainingRequests
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequests' AND COLUMN_NAME = 'BudgetSource'
)
BEGIN
    ALTER TABLE [HRDSYSTEM].[dbo].[TrainingRequests]
    ADD [BudgetSource] NVARCHAR(20) NULL;

    PRINT '✅ Added BudgetSource column to TrainingRequests';
END
ELSE
BEGIN
    PRINT '⚠️ BudgetSource column already exists in TrainingRequests';
END
GO

-- 2. สร้าง Index สำหรับ BudgetSource (ใช้ใน Dashboard query)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TrainingRequests_BudgetSource' AND object_id = OBJECT_ID('TrainingRequests')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TrainingRequests_BudgetSource]
    ON [HRDSYSTEM].[dbo].[TrainingRequests] ([BudgetSource])
    INCLUDE ([Department], [TotalCost], [Status], [StartDate], [IsActive]);

    PRINT '✅ Created index IX_TrainingRequests_BudgetSource';
END
GO

-- 3. INSERT โควต้างบกลาง CENTRAL_TRAINING_BUDGET สำหรับปีปัจจุบัน (ถ้ายังไม่มี)
IF NOT EXISTS (
    SELECT 1 FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
    WHERE [Department] = 'CENTRAL_TRAINING_BUDGET'
    AND [Year] = CAST(YEAR(GETDATE()) AS NVARCHAR(4))
)
BEGIN
    INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
    ([Department], [Year], [Cost], [Qhours], [CreatedBy], [ModifyBy])
    VALUES
    ('CENTRAL_TRAINING_BUDGET', CAST(YEAR(GETDATE()) AS NVARCHAR(4)), 0, 0, 'SYSTEM', 'SYSTEM');

    PRINT '✅ Inserted CENTRAL_TRAINING_BUDGET quota for current year (please update Cost and Qhours via QuotaManagement)';
END
ELSE
BEGIN
    PRINT '⚠️ CENTRAL_TRAINING_BUDGET quota already exists for current year';
END
GO
