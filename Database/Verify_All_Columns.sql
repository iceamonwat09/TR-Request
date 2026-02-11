-- =====================================================
-- Script: Verify All Columns - เปรียบเทียบ DB จริง vs ที่ควรมี
-- Purpose: ตรวจสอบว่า Column ทั้งหมดที่ระบบต้องการ มีครบหรือไม่
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-11
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

-- สร้าง Temp Table เก็บรายการ Column ที่ระบบต้องการ
IF OBJECT_ID('tempdb..#ExpectedColumns') IS NOT NULL
    DROP TABLE #ExpectedColumns;

CREATE TABLE #ExpectedColumns (
    TableName NVARCHAR(100),
    ColumnName NVARCHAR(100),
    DataType NVARCHAR(50),
    MaxLength NVARCHAR(20),
    IsNullable NVARCHAR(5)
);

-- =====================================================
-- 1. TrainingRequests (80 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
-- Base Columns
('TrainingRequests', 'Id', 'int', '', 'NO'),
('TrainingRequests', 'DocNo', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Company', 'nvarchar', '50', 'YES'),
('TrainingRequests', 'TrainingType', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Factory', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'CCEmail', 'nvarchar', '250', 'YES'),
('TrainingRequests', 'Position', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Department', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'EmployeeCode', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'StartDate', 'date', '', 'YES'),
('TrainingRequests', 'EndDate', 'date', '', 'YES'),
('TrainingRequests', 'SeminarTitle', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'TrainingLocation', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'Instructor', 'nvarchar', '150', 'YES'),
('TrainingRequests', 'TotalCost', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'CostPerPerson', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'PerPersonTrainingHours', 'int', '', 'YES'),
('TrainingRequests', 'RegistrationCost', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'InstructorFee', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'EquipmentCost', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'FoodCost', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'OtherCost', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'OtherCostDescription', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'TotalPeople', 'int', '', 'YES'),
('TrainingRequests', 'TrainingObjective', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'OtherObjective', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'URLSource', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'AdditionalNotes', 'nvarchar', '1000', 'YES'),
('TrainingRequests', 'ExpectedOutcome', 'nvarchar', '1000', 'YES'),
('TrainingRequests', 'AttachedFilePath', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'Status', 'nvarchar', '50', 'YES'),
-- Approval Columns
('TrainingRequests', 'SectionManagerId', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_SectionManager', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_SectionManager', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_SectionManager', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'DepartmentManagerId', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_DepartmentManager', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_DepartmentManager', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_DepartmentManager', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'ManagingDirectorId', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_ManagingDirector', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_ManagingDirector', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_ManagingDirector', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'HRDAdminid', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_HRDAdmin', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_HRDAdmin', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_HRDAdmin', 'nvarchar', '200', 'YES'),
('TrainingRequests', 'HRDConfirmationid', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_HRDConfirmation', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_HRDConfirmation', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_HRDConfirmation', 'nvarchar', '200', 'YES'),
-- Deputy Managing Director
('TrainingRequests', 'DeputyManagingDirectorId', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'Status_DeputyManagingDirector', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'Comment_DeputyManagingDirector', 'nvarchar', '500', 'YES'),
('TrainingRequests', 'ApproveInfo_DeputyManagingDirector', 'nvarchar', '200', 'YES'),
-- Audit
('TrainingRequests', 'CreatedDate', 'datetime2', '', 'YES'),
('TrainingRequests', 'CreatedBy', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'UpdatedDate', 'datetime2', '', 'YES'),
('TrainingRequests', 'UpdatedBy', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'IsActive', 'bit', '', 'YES'),
-- HRD Record
('TrainingRequests', 'HRD_ContactDate', 'date', '', 'YES'),
('TrainingRequests', 'HRD_ContactPerson', 'nvarchar', '100', 'YES'),
('TrainingRequests', 'HRD_PaymentDate', 'date', '', 'YES'),
('TrainingRequests', 'HRD_PaymentMethod', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'HRD_RecorderSignature', 'nvarchar', '100', 'YES'),
-- HRD Section 4
('TrainingRequests', 'HRD_TrainingRecord', 'bit', '', 'YES'),
('TrainingRequests', 'HRD_KnowledgeManagementDone', 'bit', '', 'YES'),
('TrainingRequests', 'HRD_CourseCertification', 'bit', '', 'YES'),
-- Knowledge Management
('TrainingRequests', 'KM_SubmitDocument', 'bit', '', 'YES'),
('TrainingRequests', 'KM_CreateReport', 'bit', '', 'YES'),
('TrainingRequests', 'KM_CreateReportDate', 'date', '', 'YES'),
('TrainingRequests', 'KM_KnowledgeSharing', 'bit', '', 'YES'),
('TrainingRequests', 'KM_KnowledgeSharingDate', 'date', '', 'YES'),
-- HRD Budget & Membership
('TrainingRequests', 'HRD_BudgetPlan', 'nvarchar', '10', 'YES'),
('TrainingRequests', 'HRD_BudgetUsage', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'BudgetSource', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'HRD_DepartmentBudgetRemaining', 'decimal', '12,2', 'YES'),
('TrainingRequests', 'HRD_MembershipType', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'HRD_MembershipCost', 'decimal', '12,2', 'YES'),
-- Travel & Target Group
('TrainingRequests', 'TravelMethod', 'nvarchar', '20', 'YES'),
('TrainingRequests', 'TargetGroup', 'nvarchar', '200', 'YES');

-- =====================================================
-- 2. TrainingRequestEmployees (14 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('TrainingRequestEmployees', 'Id', 'int', '', 'NO'),
('TrainingRequestEmployees', 'TrainingRequestId', 'int', '', 'YES'),
('TrainingRequestEmployees', 'EmployeeCode', 'nvarchar', '20', 'YES'),
('TrainingRequestEmployees', 'EmployeeName', 'nvarchar', '100', 'YES'),
('TrainingRequestEmployees', 'Position', 'nvarchar', '100', 'YES'),
('TrainingRequestEmployees', 'Department', 'nvarchar', '100', 'YES'),
('TrainingRequestEmployees', 'Level', 'nvarchar', '100', 'YES'),
('TrainingRequestEmployees', 'PreviousTrainingHours', 'int', '', 'YES'),
('TrainingRequestEmployees', 'PreviousTrainingCost', 'decimal', '10,2', 'YES'),
('TrainingRequestEmployees', 'CurrentTrainingHours', 'int', '', 'YES'),
('TrainingRequestEmployees', 'CurrentTrainingCost', 'decimal', '10,2', 'YES'),
('TrainingRequestEmployees', 'RemainingHours', 'int', '', 'YES'),
('TrainingRequestEmployees', 'RemainingCost', 'decimal', '10,2', 'YES'),
('TrainingRequestEmployees', 'Notes', 'nvarchar', '500', 'YES');

-- =====================================================
-- 3. TrainingRequestAttachments (4 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('TrainingRequestAttachments', 'ID', 'int', '', 'NO'),
('TrainingRequestAttachments', 'DocNo', 'nvarchar', '20', 'YES'),
('TrainingRequestAttachments', 'File_Name', 'nvarchar', '255', 'YES'),
('TrainingRequestAttachments', 'Modify_Date', 'nvarchar', '50', 'YES');

-- =====================================================
-- 4. TrainingRequest_Cost (7 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('TrainingRequest_Cost', 'ID', 'int', '', 'NO'),
('TrainingRequest_Cost', 'Department', 'nvarchar', '100', 'YES'),
('TrainingRequest_Cost', 'Year', 'nvarchar', '50', 'YES'),
('TrainingRequest_Cost', 'Cost', 'decimal', '12,2', 'YES'),
('TrainingRequest_Cost', 'Qhours', 'int', '', 'YES'),
('TrainingRequest_Cost', 'CreatedBy', 'nvarchar', '100', 'YES'),
('TrainingRequest_Cost', 'ModifyBy', 'nvarchar', '100', 'YES');

-- =====================================================
-- 5. RetryEmailHistory (7 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('RetryEmailHistory', 'Id', 'int', '', 'NO'),
('RetryEmailHistory', 'TrainingRequestId', 'int', '', 'YES'),
('RetryEmailHistory', 'DocNo', 'nvarchar', '50', 'YES'),
('RetryEmailHistory', 'RetryBy', 'nvarchar', '255', 'YES'),
('RetryEmailHistory', 'RetryDate', 'datetime', '', 'YES'),
('RetryEmailHistory', 'StatusAtRetry', 'nvarchar', '100', 'YES'),
('RetryEmailHistory', 'IPAddress', 'nvarchar', '50', 'YES');

-- =====================================================
-- 6. EmailLogs (10 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('EmailLogs', 'Id', 'int', '', 'NO'),
('EmailLogs', 'TrainingRequestId', 'int', '', 'YES'),
('EmailLogs', 'DocNo', 'nvarchar', '20', 'YES'),
('EmailLogs', 'RecipientEmail', 'nvarchar', '100', 'YES'),
('EmailLogs', 'EmailType', 'nvarchar', '50', 'YES'),
('EmailLogs', 'Subject', 'nvarchar', '200', 'YES'),
('EmailLogs', 'SentDate', 'datetime2', '', 'YES'),
('EmailLogs', 'Status', 'nvarchar', '20', 'YES'),
('EmailLogs', 'ErrorMessage', 'nvarchar', '1000', 'YES'),
('EmailLogs', 'RetryCount', 'int', '', 'YES');

-- =====================================================
-- 7. ApprovalHistory (11 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('ApprovalHistory', 'Id', 'int', '', 'NO'),
('ApprovalHistory', 'TrainingRequestId', 'int', '', 'NO'),
('ApprovalHistory', 'DocNo', 'nvarchar', '20', 'YES'),
('ApprovalHistory', 'ApproverRole', 'nvarchar', '50', 'NO'),
('ApprovalHistory', 'ApproverEmail', 'nvarchar', '100', 'NO'),
('ApprovalHistory', 'Action', 'nvarchar', '20', 'NO'),
('ApprovalHistory', 'Comment', 'nvarchar', '500', 'YES'),
('ApprovalHistory', 'ActionDate', 'datetime2', '', 'NO'),
('ApprovalHistory', 'PreviousStatus', 'nvarchar', '50', 'YES'),
('ApprovalHistory', 'NewStatus', 'nvarchar', '50', 'YES'),
('ApprovalHistory', 'IpAddress', 'nvarchar', '50', 'YES');

-- =====================================================
-- 8. TrainingHistory (8 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('TrainingHistory', 'Id', 'int', '', 'NO'),
('TrainingHistory', 'TrainingRequestId', 'int', '', 'NO'),
('TrainingHistory', 'EmployeeCode', 'nvarchar', '20', 'YES'),
('TrainingHistory', 'EmployeeName', 'nvarchar', '100', 'YES'),
('TrainingHistory', 'HistoryType', 'nvarchar', '20', 'YES'),
('TrainingHistory', 'TrainingDate', 'date', '', 'YES'),
('TrainingHistory', 'CourseName', 'nvarchar', '500', 'YES'),
('TrainingHistory', 'CreatedDate', 'datetime2', '', 'YES');

-- =====================================================
-- 9. TrainingParticipants (8 columns)
-- =====================================================
INSERT INTO #ExpectedColumns VALUES
('TrainingParticipants', 'Id', 'int', '', 'NO'),
('TrainingParticipants', 'TrainingRequestId', 'int', '', 'NO'),
('TrainingParticipants', 'UserID', 'nvarchar', '50', 'NO'),
('TrainingParticipants', 'Prefix', 'nvarchar', '50', 'YES'),
('TrainingParticipants', 'Name', 'nvarchar', '50', 'YES'),
('TrainingParticipants', 'Lastname', 'nvarchar', '50', 'YES'),
('TrainingParticipants', 'Level', 'nvarchar', '200', 'YES'),
('TrainingParticipants', 'AddedDate', 'datetime', '', 'NO');

-- =====================================================
-- REPORT 1: Summary - จำนวน Column ต่อตาราง
-- =====================================================
PRINT '========================================================'
PRINT '  REPORT 1: Summary per Table'
PRINT '========================================================'
PRINT ''

SELECT
    e.TableName,
    COUNT(DISTINCT e.ColumnName) AS [Expected_Columns],
    COUNT(DISTINCT c.name) AS [Actual_Columns],
    COUNT(DISTINCT e.ColumnName) - COUNT(DISTINCT c.name) AS [Missing_Count],
    CASE
        WHEN COUNT(DISTINCT e.ColumnName) = COUNT(DISTINCT c.name) THEN 'OK'
        WHEN COUNT(DISTINCT c.name) = 0 THEN 'TABLE NOT FOUND'
        ELSE 'MISSING ' + CAST(COUNT(DISTINCT e.ColumnName) - COUNT(DISTINCT c.name) AS VARCHAR) + ' COLUMNS'
    END AS [Status]
FROM #ExpectedColumns e
LEFT JOIN sys.tables t ON t.name = e.TableName
LEFT JOIN sys.columns c ON t.object_id = c.object_id AND c.name = e.ColumnName
GROUP BY e.TableName
ORDER BY e.TableName;

-- =====================================================
-- REPORT 2: Missing Columns - Column ที่ขาดหายไป
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '  REPORT 2: Missing Columns (Column ที่ยังไม่มีใน DB)'
PRINT '========================================================'
PRINT ''

SELECT
    e.TableName,
    e.ColumnName,
    e.DataType,
    e.MaxLength,
    e.IsNullable,
    'MISSING' AS [Status]
FROM #ExpectedColumns e
LEFT JOIN sys.tables t ON t.name = e.TableName
LEFT JOIN sys.columns c ON t.object_id = c.object_id AND c.name = e.ColumnName
WHERE c.name IS NULL
ORDER BY e.TableName, e.ColumnName;

-- =====================================================
-- REPORT 3: Extra Columns - Column ที่มีใน DB แต่ไม่ได้อยู่ใน Script
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '  REPORT 3: Extra Columns (Column ที่มีใน DB แต่ไม่อยู่ใน Script)'
PRINT '========================================================'
PRINT ''

SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    CASE
        WHEN ty.name IN ('nvarchar','nchar') THEN CAST(c.max_length / 2 AS VARCHAR)
        WHEN ty.name IN ('varchar','char') THEN CAST(c.max_length AS VARCHAR)
        WHEN ty.name = 'decimal' THEN CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR)
        ELSE ''
    END AS MaxLength,
    'EXTRA (not in script)' AS [Status]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN #ExpectedColumns e ON e.TableName = t.name AND e.ColumnName = c.name
WHERE t.name IN (
    'TrainingRequests', 'TrainingRequestEmployees', 'TrainingRequestAttachments',
    'TrainingRequest_Cost', 'RetryEmailHistory', 'EmailLogs',
    'ApprovalHistory', 'TrainingHistory', 'TrainingParticipants'
)
AND e.ColumnName IS NULL
ORDER BY t.name, c.column_id;

-- =====================================================
-- REPORT 4: All Columns Detail - แสดงทั้งหมดพร้อมสถานะ
-- =====================================================
PRINT ''
PRINT '========================================================'
PRINT '  REPORT 4: All Columns with Status'
PRINT '========================================================'
PRINT ''

SELECT
    e.TableName,
    e.ColumnName AS [Expected_Column],
    e.DataType AS [Expected_Type],
    e.MaxLength AS [Expected_Size],
    CASE
        WHEN c.name IS NOT NULL THEN 'EXISTS'
        ELSE '** MISSING **'
    END AS [DB_Status],
    CASE
        WHEN c.name IS NOT NULL THEN ty.name
        ELSE ''
    END AS [DB_Type],
    CASE
        WHEN c.name IS NULL THEN ''
        WHEN ty.name IN ('nvarchar','nchar') THEN CAST(c.max_length / 2 AS VARCHAR)
        WHEN ty.name IN ('varchar','char') THEN CAST(c.max_length AS VARCHAR)
        WHEN ty.name = 'decimal' THEN CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR)
        ELSE ''
    END AS [DB_Size]
FROM #ExpectedColumns e
LEFT JOIN sys.tables t ON t.name = e.TableName
LEFT JOIN sys.columns c ON t.object_id = c.object_id AND c.name = e.ColumnName
LEFT JOIN sys.types ty ON c.user_type_id = ty.user_type_id
ORDER BY e.TableName, e.ColumnName;

-- Cleanup
DROP TABLE #ExpectedColumns;

PRINT ''
PRINT '========================================================'
PRINT '  Verification Complete!'
PRINT '  Run Time: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================'
GO
