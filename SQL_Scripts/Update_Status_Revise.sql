-- ============================================================================
-- SQL Script: แก้ไข Status "Revise REJECTED" เป็น "Revise"
-- วันที่สร้าง: 2025-11-18
-- วัตถุประสงค์: อัพเดท Status ในฐานข้อมูลเพื่อให้ตรงกับมาตรฐานใหม่
-- ============================================================================

USE [HRDSYSTEM]
GO

-- ============================================================================
-- 1. ตรวจสอบข้อมูลก่อน UPDATE (สำหรับการยืนยัน)
-- ============================================================================
SELECT
    Status,
    COUNT(*) AS TotalRecords
FROM [dbo].[TrainingRequests]
WHERE Status LIKE '%Revise%' OR Status LIKE '%REJECTED%'
GROUP BY Status
ORDER BY Status;

-- ============================================================================
-- 2. แสดงรายการเอกสารที่จะถูก UPDATE
-- ============================================================================
SELECT
    Id,
    DocNo,
    SeminarTitle,
    Status,
    CreatedDate,
    CreatedBy
FROM [dbo].[TrainingRequests]
WHERE Status = 'Revise REJECTED' OR Status = 'REVISE REJECTED'
ORDER BY CreatedDate DESC;

-- ============================================================================
-- 3. UPDATE Status จาก "Revise REJECTED" เป็น "Revise"
-- ============================================================================
BEGIN TRANSACTION;

-- อัพเดท Status
UPDATE [dbo].[TrainingRequests]
SET Status = 'Revise'
WHERE Status = 'Revise REJECTED'
   OR Status = 'REVISE REJECTED'
   OR Status LIKE '%Revise%REJECTED%';

-- ตรวจสอบจำนวนที่ถูก UPDATE
PRINT 'จำนวนเอกสารที่ถูกอัพเดท: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- ============================================================================
-- 4. ตรวจสอบผลลัพธ์หลัง UPDATE
-- ============================================================================
SELECT
    Status,
    COUNT(*) AS TotalRecords
FROM [dbo].[TrainingRequests]
WHERE Status LIKE '%Revise%' OR Status LIKE '%REJECTED%'
GROUP BY Status
ORDER BY Status;

-- ============================================================================
-- 5. Commit Transaction (ถ้าผลลัพธ์ถูกต้อง)
-- ============================================================================
-- *** หากตรวจสอบแล้วถูกต้อง ให้ใช้คำสั่งนี้ ***
COMMIT TRANSACTION;

-- *** หากผลลัพธ์ไม่ถูกต้อง ให้ใช้คำสั่งนี้เพื่อ Rollback ***
-- ROLLBACK TRANSACTION;

-- ============================================================================
-- 6. ตรวจสอบ Status ทั้งหมดในระบบ
-- ============================================================================
SELECT
    Status,
    COUNT(*) AS TotalRecords
FROM [dbo].[TrainingRequests]
WHERE IsActive = 1
GROUP BY Status
ORDER BY Status;

GO
