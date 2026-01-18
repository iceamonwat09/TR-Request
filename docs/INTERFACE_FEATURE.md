# Training Interface Feature Documentation

## Overview

The **Interface** feature allows HRD Admin users to send approved training request data from the TR-Request system to external training management tables (Course, OpenCourse, TimeStramp) in the HRDSYSTEM database.

---

## Feature Access

- **Menu Location**: Sidebar > Interface (visible only for Admin/HRD users)
- **URL**: `/Home/Interface`
- **Required Role**: Admin, HRD, or System

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           TR-Request System                                  │
│  ┌──────────────────────┐    ┌────────────────────────────┐                 │
│  │  TrainingRequests    │    │ TrainingRequestEmployees   │                 │
│  │  - DocNo             │───▶│ - TrainingRequestId        │                 │
│  │  - SeminarTitle      │    │ - EmployeeCode             │                 │
│  │  - StartDate         │    └────────────────────────────┘                 │
│  │  - TrainingLocation  │                 │                                  │
│  │  - Instructor        │                 │ Lookup                           │
│  │  - Company           │                 ▼                                  │
│  │  - Status            │    ┌────────────────────────────┐                 │
│  └──────────────────────┘    │      Employees             │                 │
│                              │ - UserID (= EmployeeCode)  │                 │
│                              │ - ID_emp (for TimeStramp)  │                 │
│                              │ - Name, lastname           │                 │
│                              │ - Email                    │                 │
│                              └────────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        │ SendInterfaceData API
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HRDSYSTEM External Tables                             │
│                                                                              │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────────────┐       │
│  │    Course    │      │  OpenCourse  │      │     TimeStramp       │       │
│  │              │      │              │      │                      │       │
│  │ ID (auto)────┼─────▶│ OCID         │      │ OID ◀────────────────┼───┐   │
│  │ CID          │      │ OID (auto)───┼──────┼──────────────────────┼───┘   │
│  │ CName        │      │ OOpenDate    │      │ Emp (numeric)        │       │
│  │              │      │ OLO          │      │ check_pass           │       │
│  │              │      │ time         │      │ Expert               │       │
│  │              │      │ Course_      │      │ Company              │       │
│  │              │      │   Provider   │      │ datetime_in          │       │
│  │              │      │              │      │ datetime_out         │       │
│  │              │      │              │      │ Gen                  │       │
│  │              │      │              │      │ SYear                │       │
│  │              │      │              │      │ Check_in             │       │
│  └──────────────┘      └──────────────┘      └──────────────────────┘       │
│                                                                              │
│  Relationship: 1 Course : 1 OpenCourse : N TimeStramp (per employee)        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Mapping

### Source → Destination Mapping

| Source (TrainingRequests) | Destination Table | Destination Column | Notes |
|---------------------------|-------------------|-------------------|-------|
| DocNo | Course | CID | varchar(50) |
| SeminarTitle | Course | CName | varchar(200) |
| Course.ID (auto) | OpenCourse | OCID | numeric(18,0) - FK to Course |
| StartDate | OpenCourse | OOpenDate | date |
| TrainingLocation | OpenCourse | OLO | varchar(50) - truncate if > 50 |
| Calculated Hours (TimeOut - TimeIn) | OpenCourse | time | varchar(50) - integer string |
| "Interface" | OpenCourse | Course_Provider | nvarchar(MAX) - fixed value |
| OpenCourse.OID (auto) | TimeStramp | OID | numeric(18,0) - FK to OpenCourse |
| Employees.ID_emp | TimeStramp | Emp | numeric(18,0) - lookup from Employees |
| "Pass" | TimeStramp | check_pass | varchar(50) - fixed value |
| Instructor | TimeStramp | Expert | nvarchar(50) - truncate if > 50 |
| Company | TimeStramp | Company | nvarchar(50) - truncate if > 50 |
| User Input (TimeIn) | TimeStramp | datetime_in | time(7) |
| User Input (TimeOut) | TimeStramp | datetime_out | time(7) |
| User Input (Gen) | TimeStramp | Gen | nvarchar(50) - string |
| StartDate.Year | TimeStramp | SYear | int |
| Logged-in User's Name | TimeStramp | Check_in | nvarchar(50) - "Name Lastname" |

### Employee Lookup Process

```
EmployeeCode (from TrainingRequestEmployees)
       │
       ▼
Employees.UserID = EmployeeCode
       │
       ▼
Get Employees.ID_emp → TimeStramp.Emp (as numeric)
```

### Check_in Value Lookup

```
UserEmail (from Session)
       │
       ▼
Employees.Email = UserEmail
       │
       ▼
Get Employees.Name + " " + Employees.lastname → TimeStramp.Check_in
```

---

## Database Schema

### Course Table (dbo.Course)

| Column | Data Type | Allow Nulls | Description |
|--------|-----------|-------------|-------------|
| ID | numeric(18,0) | NO | Primary Key (auto-increment) |
| CID | varchar(50) | YES | Course ID (= DocNo) |
| CName | varchar(200) | YES | Course Name (= SeminarTitle) |
| Organizer | varchar(50) | YES | Not used |
| Cost | int | YES | Not used |
| Qty | int | YES | Not used |
| Period | varchar(50) | YES | Not used |
| Object | varchar(50) | YES | Not used |
| Remark | text | YES | Not used |

### OpenCourse Table (dbo.OpenCourse)

| Column | Data Type | Allow Nulls | Description |
|--------|-----------|-------------|-------------|
| OID | numeric(18,0) | NO | Primary Key (auto-increment) |
| OLO | varchar(50) | YES | Location (= TrainingLocation) |
| OGen | varchar(50) | YES | Not used |
| OCost | varchar(50) | YES | Not used |
| OCID | numeric(18,0) | YES | FK to Course.ID |
| OOpenDate | date | YES | Training Date (= StartDate) |
| Recive | varchar(50) | YES | Not used |
| Language | varchar(50) | YES | NULL |
| categoryC | varchar(50) | YES | NULL |
| datetime_in | time(7) | YES | Not used in OpenCourse |
| datetime_out | time(7) | YES | Not used in OpenCourse |
| time | varchar(50) | YES | Training hours (integer string) |
| Examiner | nvarchar(50) | YES | Not used |
| Quiz_link | nvarchar(MAX) | YES | Not used |
| Course_Provider | nvarchar(MAX) | YES | = "Interface" |

### TimeStramp Table (dbo.TimeStramp)

| Column | Data Type | Allow Nulls | Description |
|--------|-----------|-------------|-------------|
| Emp | numeric(18,0) | NO | Employee ID (= Employees.ID_emp) |
| OID | numeric(18,0) | NO | FK to OpenCourse.OID |
| Date | varchar(50) | YES | Not used |
| Mtime | time(7) | YES | Not used |
| Atime | time(7) | YES | Not used |
| check_pass | varchar(50) | YES | = "Pass" |
| Expert | nvarchar(50) | YES | Instructor name |
| Examiner | nvarchar(50) | YES | NULL |
| TranslatorName | nvarchar(50) | YES | NULL |
| Company | nvarchar(50) | YES | Company name |
| Gen | nvarchar(50) | YES | Generation/Batch number (string) |
| datetime_in | time(7) | YES | Training start time |
| datetime_out | time(7) | YES | Training end time |
| SYear | int | YES | Year from StartDate |
| Check_in | nvarchar(50) | YES | User's full name who sent data |
| ApproverName | nvarchar(50) | YES | Not used |

**Note**: TimeStramp table has NO primary key.

---

## API Endpoints

### 1. GET /Home/Interface

**Purpose**: Render the Interface page

**Access Control**: Requires login + Admin/HRD/System role

**Response**: Interface.cshtml view

---

### 2. GET /Home/GetInterfaceRequests

**Purpose**: Fetch training requests for display in Interface table

**Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Filter start date (default: first day of current month) |
| endDate | DateTime | No | Filter end date (default: last day of current month) |
| docNo | string | No | Filter by DocNo (LIKE search) |
| company | string | No | Filter by Company (exact match) |

**Filter Conditions**:
```sql
WHERE tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED')
  AND tr.TrainingType = 'Public'
  AND tr.IsActive = 1
  AND CAST(tr.StartDate AS DATE) BETWEEN @StartDate AND @EndDate
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": 123,
      "docNo": "TR-2026-001",
      "seminarTitle": "Course Name",
      "startDate": "2026-01-15",
      "trainingLocation": "Bangkok",
      "instructor": "John Doe",
      "company": "F2-สมุทรสาคร",
      "status": "APPROVED",
      "employeeCount": 5
    }
  ]
}
```

---

### 3. POST /Home/SendInterfaceData

**Purpose**: Send training data to external tables (Course, OpenCourse, TimeStramp)

**Request Body**:
```json
{
  "trainingRequestId": 123,
  "timeIn": "08:00",
  "timeOut": "17:00",
  "gen": 1
}
```

**Process Flow**:

1. **Validate** training request exists and status is APPROVED or RESCHEDULED
2. **Insert to Course** table:
   - CID = DocNo
   - CName = SeminarTitle
   - Returns: Course.ID (auto-generated)
3. **Insert to OpenCourse** table:
   - OCID = Course.ID
   - OOpenDate = StartDate
   - OLO = TrainingLocation (truncate to 50 chars)
   - time = calculated hours (integer string)
   - Course_Provider = "Interface"
   - Returns: OpenCourse.OID (auto-generated)
4. **Lookup User's Name** from Employees by Email
5. **Get Employee List** with ID_emp lookup:
   - Join TrainingRequestEmployees with Employees on EmployeeCode = UserID
6. **Insert to TimeStramp** for each employee:
   - OID = OpenCourse.OID
   - Emp = Employees.ID_emp (as numeric)
   - check_pass = "Pass"
   - Expert = Instructor
   - Company = Company
   - datetime_in = TimeIn
   - datetime_out = TimeOut
   - Gen = Generation (as string)
   - SYear = Year from StartDate
   - Check_in = User's "Name Lastname"
7. **Update TrainingRequests** status to 'COMPLETE'

**Transaction**: All operations are wrapped in a SQL transaction. If any step fails, all changes are rolled back.

**Response**:
```json
{
  "success": true,
  "message": "ส่งข้อมูลสำเร็จ! บันทึกพนักงาน 5 คน",
  "courseId": 456,
  "openCourseId": 789,
  "employeeCount": 5
}
```

---

## Frontend Components (Interface.cshtml)

### Filter Section

- **Start Date**: Date picker (default: first day of current month)
- **End Date**: Date picker (default: last day of current month)
- **DocNo**: Text input for searching DocNo
- **Company**: Dropdown with options:
  - All Companies
  - F2-สมุทรสาคร
  - F4-ปราจีนบุรี

### Status Cards

Displays count of records by status:
- APPROVED (green)
- COMPLETE (blue)
- RESCHEDULED (yellow)

### Data Table

Columns:
1. DocNo
2. Seminar Title
3. Start Date
4. Location
5. Instructor
6. Company
7. Employees (count)
8. Status (badge)
9. Action (Send button)

**Send Button States**:
- **Enabled**: For APPROVED and RESCHEDULED status
- **Disabled**: For COMPLETE status (already sent)

### Send Data Modal

Input fields:
- **Training Start Time** (default: 08:00)
- **Training End Time** (default: 17:00)
- **Training Hours** (auto-calculated, display only)
- **Generation** (dropdown: 1-10, default: 1)

---

## Business Rules

1. **Only Public Training**: Interface only shows TrainingType = 'Public'

2. **Status Restriction**: Only APPROVED, COMPLETE, RESCHEDULED status records are shown

3. **Send Once**: Records with COMPLETE status cannot be sent again (button disabled)

4. **Employee ID Mapping**: EmployeeCode must be looked up in Employees.UserID to get Employees.ID_emp for TimeStramp.Emp (numeric field)

5. **One-to-Many Relationship**:
   - 1 TrainingRequest → 1 Course record
   - 1 Course → 1 OpenCourse record
   - 1 OpenCourse → N TimeStramp records (one per employee)

6. **Data Truncation**: All varchar(50) fields are truncated to max 50 characters to prevent SQL errors

7. **Training Hours**: Calculated as integer (rounded) from TimeOut - TimeIn

8. **Check_in Value**: User's full name from Employees table (Name + " " + Lastname), not email

---

## Error Handling

- **String Truncation**: All string fields are truncated to fit column size limits
- **Invalid Employee ID**: If ID_emp cannot be parsed as numeric, that employee is skipped
- **Transaction Rollback**: Any error during insert operations causes full rollback
- **Console Logging**: Detailed logging for debugging with emoji indicators

---

## File Locations

| File | Purpose |
|------|---------|
| `Controllers/HomeController.cs` | API endpoints: Interface(), GetInterfaceRequests(), SendInterfaceData() |
| `Views/Home/Interface.cshtml` | Frontend UI with filter, table, modal |
| `Views/Home/Index.cshtml` | Sidebar menu with Interface link |

---

## Related Tables in TR-Request System

- **TrainingRequests**: Main training request data
- **TrainingRequestEmployees**: Employees assigned to each training
- **Employees**: Employee master data (UserID, ID_emp, Name, lastname, Email)

---

## Version History

| Date | Change |
|------|--------|
| 2026-01-17 | Initial implementation |
| 2026-01-17 | Added filter section (date range, DocNo, Company) |
| 2026-01-17 | Fixed data types for Course, OpenCourse, TimeStramp |
| 2026-01-17 | Changed OpenCourse.time to integer |
| 2026-01-17 | Changed Check_in to user's full name from Employees |
| 2026-01-17 | Added TrainingType = 'Public' filter |
