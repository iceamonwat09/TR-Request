-- ตรวจสอบ Schema ของ TrainingRequests table
-- เพื่อดูว่ามี UpdatedDate column หรือไม่

USE [HRDSYSTEM];
GO

-- ตรวจสอบ Columns ทั้งหมดใน TrainingRequests
PRINT '=== TrainingRequests Table Columns ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
ORDER BY ORDINAL_POSITION;
GO

-- ตรวจสอบว่ามี UpdatedDate หรือไม่
PRINT '';
PRINT '=== Checking UpdatedDate Column ===';
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequests'
    AND COLUMN_NAME = 'UpdatedDate'
)
    PRINT '✅ UpdatedDate column EXISTS'
ELSE
    PRINT '❌ UpdatedDate column NOT FOUND - This is the problem!';
GO

-- ทดสอบ UPDATE command ที่ StartWorkflow ใช้
PRINT '';
PRINT '=== Test UPDATE Command ===';
BEGIN TRY
    UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
    SET Status = 'WAITING_FOR_SECTION_MANAGER', UpdatedDate = GETDATE()
    WHERE DocNo = 'PB-2025-11-032';

    PRINT '✅ UPDATE command successful';

    -- แสดงผลลัพธ์
    SELECT DocNo, Status, UpdatedDate
    FROM TrainingRequests
    WHERE DocNo = 'PB-2025-11-032';
END TRY
BEGIN CATCH
    PRINT '❌ UPDATE command FAILED!';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
END CATCH;
GO
