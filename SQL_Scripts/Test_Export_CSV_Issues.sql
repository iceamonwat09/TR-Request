-- =====================================================
-- 🔍 SQL Query สำหรับทดสอบปัญหา Export CSV
-- วันที่: 2025-12-30
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '========================================';
PRINT '📊 เริ่มการทดสอบ Export CSV Function';
PRINT '========================================';
PRINT '';

-- =====================================================
-- ✅ TEST 1: ตรวจสอบจำนวน Records ทั้งหมด
-- =====================================================
PRINT '1️⃣ ตรวจสอบจำนวน Records ทั้งหมด';
PRINT '----------------------------------------';

-- จำนวน TrainingRequests ทั้งหมด
SELECT
    COUNT(*) AS TotalTrainingRequests,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveRequests,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveRequests
FROM [TrainingRequests];

-- จำนวน TrainingRequestEmployees ทั้งหมด
SELECT
    COUNT(*) AS TotalEmployeeRecords,
    COUNT(DISTINCT TrainingRequestId) AS UniqueTrainingRequests
FROM [TrainingRequestEmployees];

PRINT '';
PRINT '';

-- =====================================================
-- ⚠️ TEST 2: ทดสอบปัญหา INNER JOIN vs LEFT JOIN
-- =====================================================
PRINT '2️⃣ ทดสอบปัญหา INNER JOIN vs LEFT JOIN';
PRINT '----------------------------------------';

-- 2.1 หา TrainingRequests ที่ไม่มี Employee data
PRINT '🔍 TrainingRequests ที่ไม่มีข้อมูลพนักงาน (จะหายใน INNER JOIN):';
SELECT
    tr.Id,
    tr.DocNo,
    tr.Department,
    tr.SeminarTitle,
    tr.StartDate,
    tr.Status,
    tr.TotalCost,
    tr.CreatedDate,
    tr.CreatedBy
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE emp.Id IS NULL
  AND tr.IsActive = 1
ORDER BY tr.CreatedDate DESC;

-- นับจำนวนที่จะหาย
SELECT
    COUNT(*) AS RecordsWillBeLostWithInnerJoin
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE emp.Id IS NULL
  AND tr.IsActive = 1;

PRINT '';
PRINT '';

-- =====================================================
-- 📊 TEST 3: เปรียบเทียบผลลัพธ์ INNER JOIN vs LEFT JOIN
-- =====================================================
PRINT '3️⃣ เปรียบเทียบจำนวน Records: INNER JOIN vs LEFT JOIN';
PRINT '----------------------------------------';

-- 3.1 จำนวนจาก INNER JOIN (Query ปัจจุบัน)
DECLARE @InnerJoinCount INT;
SELECT @InnerJoinCount = COUNT(*)
FROM [TrainingRequests] tr
INNER JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1;

PRINT 'INNER JOIN (Query ปัจจุบัน): ' + CAST(@InnerJoinCount AS NVARCHAR(10)) + ' rows';

-- 3.2 จำนวนจาก LEFT JOIN (ที่ควรจะเป็น)
DECLARE @LeftJoinCount INT;
SELECT @LeftJoinCount = COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1;

PRINT 'LEFT JOIN (ที่ควรจะเป็น): ' + CAST(@LeftJoinCount AS NVARCHAR(10)) + ' rows';
PRINT 'ผลต่าง (ข้อมูลที่หายไป): ' + CAST(@LeftJoinCount - @InnerJoinCount AS NVARCHAR(10)) + ' rows';

PRINT '';
PRINT '';

-- =====================================================
-- 📅 TEST 4: ทดสอบ Filter Parameters (ตามที่ใช้จริง)
-- =====================================================
PRINT '4️⃣ ทดสอบ Filter Parameters';
PRINT '----------------------------------------';

-- ตัวอย่าง: Filter ปี 2025
DECLARE @TestYear NVARCHAR(4) = '2025';
DECLARE @TestStartDate DATE = '2025-01-01';
DECLARE @TestEndDate DATE = '2025-12-31';

SELECT
    'ปี 2025' AS FilterCondition,
    COUNT(*) AS TotalRecords
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @TestStartDate
  AND tr.StartDate <= @TestEndDate
  AND tr.IsActive = 1;

-- ตัวอย่าง: Filter ฝ่าย
SELECT
    tr.Department,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN emp.Id IS NULL THEN 1 ELSE 0 END) AS RecordsWithoutEmployee
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @TestStartDate
  AND tr.StartDate <= @TestEndDate
  AND tr.IsActive = 1
GROUP BY tr.Department
ORDER BY TotalRecords DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 🔍 TEST 5: ตรวจสอบความถูกต้องของข้อมูล
-- =====================================================
PRINT '5️⃣ ตรวจสอบความถูกต้องของข้อมูล';
PRINT '----------------------------------------';

-- 5.1 ตรวจสอบ NULL values ใน Column สำคัญ
SELECT
    'DocNo NULL' AS Issue,
    COUNT(*) AS Count
FROM [TrainingRequests]
WHERE DocNo IS NULL AND IsActive = 1
UNION ALL
SELECT
    'Department NULL' AS Issue,
    COUNT(*) AS Count
FROM [TrainingRequests]
WHERE Department IS NULL AND IsActive = 1
UNION ALL
SELECT
    'SeminarTitle NULL' AS Issue,
    COUNT(*) AS Count
FROM [TrainingRequests]
WHERE SeminarTitle IS NULL AND IsActive = 1
UNION ALL
SELECT
    'EmployeeCode NULL in Employees' AS Issue,
    COUNT(*) AS Count
FROM [TrainingRequestEmployees]
WHERE EmployeeCode IS NULL;

-- 5.2 ตรวจสอบ TrainingRequest ที่มี Employee หลายคน
SELECT
    tr.Id,
    tr.DocNo,
    tr.SeminarTitle,
    COUNT(emp.Id) AS NumberOfEmployees
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1
GROUP BY tr.Id, tr.DocNo, tr.SeminarTitle
HAVING COUNT(emp.Id) > 1
ORDER BY NumberOfEmployees DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 🎯 TEST 6: ทดสอบ Query แบบเต็ม (ตาม Export Function จริง)
-- =====================================================
PRINT '6️⃣ ทดสอบ Query แบบเต็ม (ตาม Export Function)';
PRINT '----------------------------------------';

-- 6.1 INNER JOIN (Query ปัจจุบัน - มีปัญหา)
PRINT '🔴 INNER JOIN (Query ปัจจุบัน):';
SELECT TOP 5
    tr.DocNo,
    tr.Company,
    tr.TrainingType,
    tr.Department,
    tr.SeminarTitle,
    tr.TotalCost,
    emp.EmployeeCode,
    emp.EmployeeName
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1
ORDER BY tr.CreatedDate DESC;

-- 6.2 LEFT JOIN (Query ที่ควรจะเป็น - แก้ไขแล้ว)
PRINT '';
PRINT '✅ LEFT JOIN (Query ที่ควรจะเป็น):';
SELECT TOP 5
    tr.DocNo,
    tr.Company,
    tr.TrainingType,
    tr.Department,
    tr.SeminarTitle,
    tr.TotalCost,
    emp.EmployeeCode,
    emp.EmployeeName,
    CASE
        WHEN emp.Id IS NULL THEN '⚠️ ไม่มีข้อมูลพนักงาน'
        ELSE '✅ มีข้อมูลพนักงาน'
    END AS EmployeeDataStatus
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1
ORDER BY tr.CreatedDate DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 📈 TEST 7: ตรวจสอบ Performance (จำนวน Records)
-- =====================================================
PRINT '7️⃣ ตรวจสอบ Performance';
PRINT '----------------------------------------';

-- จำนวน Records ตามช่วงเวลา
SELECT
    YEAR(StartDate) AS Year,
    COUNT(*) AS TotalRequests,
    SUM(CASE WHEN Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN 1 ELSE 0 END) AS ApprovedRequests
FROM [TrainingRequests]
WHERE IsActive = 1
GROUP BY YEAR(StartDate)
ORDER BY Year DESC;

-- ประมาณการ Export Size
SELECT
    COUNT(*) AS TotalExportRows,
    COUNT(*) * 45 AS ApproximateColumns,
    CASE
        WHEN COUNT(*) < 1000 THEN '✅ ปลอดภัย'
        WHEN COUNT(*) BETWEEN 1000 AND 10000 THEN '⚠️ ปานกลาง'
        WHEN COUNT(*) BETWEEN 10000 AND 100000 THEN '🟠 ควรระวัง'
        ELSE '🔴 อันตราย - ควรใช้ Paging'
    END AS PerformanceRisk
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1;

PRINT '';
PRINT '';

-- =====================================================
-- 🔍 TEST 8: ตรวจสอบ Column Name Case Sensitivity
-- =====================================================
PRINT '8️⃣ ตรวจสอบ Column Schema';
PRINT '----------------------------------------';

-- ตรวจสอบชื่อ Column จริงในตาราง
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequestEmployees'
  AND COLUMN_NAME IN ('Level', 'level', 'LEVEL')
ORDER BY TABLE_NAME, COLUMN_NAME;

PRINT '';
PRINT '';

-- =====================================================
-- 📝 TEST 9: ตัวอย่างข้อมูลที่จะ Export (Preview)
-- =====================================================
PRINT '9️⃣ ตัวอย่างข้อมูลที่จะ Export (Top 3)';
PRINT '----------------------------------------';

SELECT TOP 3
    tr.DocNo AS [เลขที่เอกสาร],
    tr.Company AS [บริษัท],
    tr.TrainingType AS [ประเภทการอบรม],
    tr.Department AS [ฝ่าย],
    tr.SeminarTitle AS [หัวข้ออบรม],
    tr.TotalCost AS [ค่าใช้จ่ายรวม],
    tr.Status AS [สถานะ],
    emp.EmployeeCode AS [รหัสพนักงาน],
    emp.EmployeeName AS [ชื่อพนักงาน],
    emp.Position AS [ตำแหน่ง],
    CASE
        WHEN emp.Id IS NULL THEN '⚠️ Record นี้จะหายถ้าใช้ INNER JOIN'
        ELSE '✅ Record นี้จะแสดงปกติ'
    END AS [Status_Note]
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1
ORDER BY tr.CreatedDate DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 📊 สรุปผลการทดสอบ
-- =====================================================
PRINT '========================================';
PRINT '📊 สรุปผลการทดสอบ';
PRINT '========================================';

DECLARE @TotalTR INT, @TotalEmp INT, @LostRecords INT, @TotalExport INT;

SELECT @TotalTR = COUNT(*) FROM [TrainingRequests] WHERE IsActive = 1;
SELECT @TotalEmp = COUNT(DISTINCT TrainingRequestId) FROM [TrainingRequestEmployees];
SELECT @LostRecords = COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE emp.Id IS NULL AND tr.IsActive = 1;
SELECT @TotalExport = COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1;

PRINT '';
PRINT 'จำนวน TrainingRequests (Active): ' + CAST(@TotalTR AS NVARCHAR(10));
PRINT 'จำนวน TrainingRequests ที่มี Employee: ' + CAST(@TotalEmp AS NVARCHAR(10));
PRINT 'จำนวนที่จะหายถ้าใช้ INNER JOIN: ' + CAST(@LostRecords AS NVARCHAR(10));
PRINT 'จำนวนที่ควร Export (LEFT JOIN): ' + CAST(@TotalExport AS NVARCHAR(10));
PRINT '';

IF @LostRecords > 0
BEGIN
    PRINT '🔴 คำเตือน: พบข้อมูล ' + CAST(@LostRecords AS NVARCHAR(10)) + ' รายการที่จะหายถ้าใช้ INNER JOIN';
    PRINT '✅ แนะนำ: ควรเปลี่ยนเป็น LEFT JOIN';
END
ELSE
BEGIN
    PRINT '✅ ดี: ข้อมูลทุกรายการมี Employee';
END

PRINT '';
PRINT '========================================';
PRINT '✅ ทดสอบเสร็จสิ้น';
PRINT '========================================';
