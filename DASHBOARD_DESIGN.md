# 📊 Training Request Dashboard - Professional Analytics Design

**วันที่:** 2025-11-18
**เวอร์ชัน:** 2.0 - Interactive Analytics Dashboard
**ออกแบบโดย:** Data Analyst Perspective

---

## 🎯 วัตถุประสงค์

สร้าง Dashboard แบบ **Interactive Analytics** ที่:
- ✅ แสดงข้อมูลจริงจากฐานข้อมูล (Real-time)
- ✅ วิเคราะห์ข้อมูลได้หลายมิติ (Multi-dimensional Analysis)
- ✅ Cross-filtering แบบ Power BI (Click-through Interaction)
- ✅ Date Range Filtering (สำหรับ Admin)
- ✅ Drill-down ไปยังรายละเอียด (Detail View)
- ✅ Export ข้อมูล (Excel/PDF)
- ✅ ไม่กระทบโครงสร้างการทำงานเดิม

---

## 📐 Dashboard Layout Design

```
┌─────────────────────────────────────────────────────────────────────────┐
│  🏠 Training Request Analytics Dashboard                    [Admin]     │
│  📅 Year: [2025 ▼]  📆 Date Range: [01/01/2025] - [31/12/2025]  [Apply]│
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐      │
│  │ 💰 Total    │ │ 📊 Budget   │ │ 📝 Total    │ │ ✅ Approved │      │
│  │    Cost     │ │   Usage     │ │   Requests  │ │   Rate      │      │
│  │             │ │             │ │             │ │             │      │
│  │  2.5M บาท  │ │  68.5% 🔴  │ │    156      │ │   78.2%     │      │
│  │  ↑ +12.3%  │ │  เกินโควต้า │ │  ↑ +23      │ │  ↑ +5.1%   │      │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘      │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────┐ ┌────────────────────────────┐  │
│  │ 📊 งบประมาณแต่ละฝ่าย vs โควต้า │ │ 🥧 สถานะเอกสาร           │  │
│  │  (Bar Chart - Interactive)      │ │    (Donut Chart)          │  │
│  │                                  │ │                            │  │
│  │  IT Dept    ████████ 85%        │ │  Approved    45%           │  │
│  │  HR Dept    █████ 62%           │ │  Pending     20%           │  │
│  │  Sales Dept ██████████ 92% 🔴  │ │  Waiting     25%           │  │
│  │  Marketing  ███ 45%             │ │  Rejected    10%           │  │
│  │                                  │ │                            │  │
│  │  [Click เพื่อ Filter ข้อมูล]   │ │  [Click เพื่อดูรายละเอียด]│  │
│  └──────────────────────────────────┘ └────────────────────────────┘  │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ 📈 Trend การขออบรมรายเดือน (Line Chart - Interactive)           │  │
│  │                                                                  │  │
│  │      │                                      ●                    │  │
│  │  20  │                  ●                 ● │ ●                 │  │
│  │  15  │        ●       ●   ●             ●   │   ●               │  │
│  │  10  │    ●     ●   ●       ●         ●     │     ●             │  │
│  │   5  │  ●                     ●     ●       │                   │  │
│  │   0  └─────────────────────────────────────────────────────────  │  │
│  │      Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec            │  │
│  │                                                                  │  │
│  │  [Hover เพื่อดูรายละเอียด] [Click เพื่อ Drill-down]            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────┐ ┌─────────────────────────────────────┐   │
│  │ 🏆 Top 5 Departments   │ │ ⏱️ Processing Time Analysis        │   │
│  │    (by Cost)           │ │    (Average Days)                  │   │
│  │                        │ │                                     │   │
│  │ 1. IT          850K    │ │  Pending → Approved:    3.5 days   │   │
│  │ 2. Sales       720K    │ │  HRD Review:            2.1 days   │   │
│  │ 3. Marketing   650K    │ │  Manager Approval:      1.8 days   │   │
│  │ 4. HR          540K    │ │  Total Average:         7.4 days   │   │
│  │ 5. Finance     480K    │ │                                     │   │
│  └────────────────────────┘ └─────────────────────────────────────┘   │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  📋 Recent Training Requests (Filtered Data)                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ DocNo    │ Dept  │ Cost   │ Status   │ Date       │ Actions   │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │ TR-2025  │ IT    │ 50,000 │ Approved │ 2025-11-15 │ [View]    │   │
│  │ TR-2024  │ Sales │ 35,000 │ Pending  │ 2025-11-14 │ [View]    │   │
│  │ ...      │       │        │          │            │           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  [Export to Excel] [Export to PDF] [View All →]                       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🎨 KPI Cards (4 หลัก)

### 1. **💰 Total Cost (ค่าใช้จ่ายรวม)**
```javascript
{
    value: "2,548,320 บาท",
    trend: "+12.3%",
    comparison: "vs ปีที่แล้ว",
    color: "success",
    icon: "fa-money-bill-wave"
}
```

**การคำนวณ:**
```sql
SELECT SUM(TotalCost)
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
  AND Status IN ('APPROVED', 'COMPLETE')
```

---

### 2. **📊 Budget Usage (% การใช้งบ)**
```javascript
{
    value: "68.5%",
    quota: "3,000,000 บาท",
    used: "2,055,000 บาท",
    remaining: "945,000 บาท",
    status: "warning", // หรือ "danger" ถ้าเกิน 100%
    icon: "fa-chart-pie"
}
```

**การคำนวณ:**
```sql
-- Total Used
SELECT SUM(TotalCost) FROM TrainingRequests
WHERE YEAR(StartDate) = @Year AND Status IN ('APPROVED', 'COMPLETE')

-- Total Quota
SELECT SUM(Cost) FROM TrainingRequest_Cost WHERE Year = @Year

-- Percentage = (Used / Quota) * 100
```

---

### 3. **📝 Total Requests (จำนวนคำขอทั้งหมด)**
```javascript
{
    value: "156",
    trend: "+23",
    breakdown: {
        approved: 78,
        pending: 31,
        waiting: 39,
        rejected: 8
    },
    icon: "fa-file-alt"
}
```

**การคำนวณ:**
```sql
SELECT COUNT(*) FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
```

---

### 4. **✅ Approval Rate (อัตราการอนุมัติ)**
```javascript
{
    value: "78.2%",
    trend: "+5.1%",
    approved: 122,
    total: 156,
    color: "success",
    icon: "fa-check-circle"
}
```

**การคำนวณ:**
```sql
-- Approval Rate = (Approved / Total) * 100
SELECT
    (CAST(COUNT(CASE WHEN Status IN ('APPROVED','COMPLETE') THEN 1 END) AS FLOAT) / COUNT(*)) * 100 AS ApprovalRate
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
```

---

## 📊 Charts & Visualizations

### **Chart 1: งบประมาณแต่ละฝ่าย vs โควต้า (Bar Chart)**

**Type:** Horizontal Stacked Bar Chart
**Interactive:** ✅ Click to Filter
**Library:** Chart.js / ApexCharts

**Data Structure:**
```javascript
{
    categories: ["IT", "HR", "Sales", "Marketing", "Finance"],
    series: [
        {
            name: "ใช้ไป",
            data: [850000, 540000, 720000, 650000, 480000]
        },
        {
            name: "คงเหลือ",
            data: [150000, 460000, -20000, 350000, 520000] // ติดลบ = เกินโควต้า
        }
    ],
    quota: [1000000, 1000000, 700000, 1000000, 1000000]
}
```

**SQL Query:**
```sql
SELECT
    e.Department,
    ISNULL(SUM(tr.TotalCost), 0) AS TotalUsed,
    qc.Cost AS Quota,
    (qc.Cost - ISNULL(SUM(tr.TotalCost), 0)) AS Remaining,
    (ISNULL(SUM(tr.TotalCost), 0) / qc.Cost * 100) AS UsagePercent
FROM [TrainingRequest_Cost] qc
LEFT JOIN TrainingRequests tr ON tr.Department = qc.Department
    AND YEAR(tr.StartDate) = qc.Year
    AND tr.Status IN ('APPROVED', 'COMPLETE')
LEFT JOIN Employees e ON e.Department = qc.Department
WHERE qc.Year = @Year
GROUP BY e.Department, qc.Cost
ORDER BY UsagePercent DESC
```

**Interactive Features:**
- ✅ Click Department → Filter all charts
- ✅ Hover → Show tooltip (Used, Quota, Remaining)
- ✅ Color code: Green (<80%), Yellow (80-100%), Red (>100%)

---

### **Chart 2: สถานะเอกสาร (Donut Chart)**

**Type:** Donut Chart
**Interactive:** ✅ Click to Drill-down
**Library:** Chart.js / ApexCharts

**Data Structure:**
```javascript
{
    labels: [
        "Approved",
        "Pending",
        "Waiting for Approval",
        "Rejected",
        "Revise",
        "Complete"
    ],
    data: [45, 20, 25, 5, 3, 2],
    colors: ["#28a745", "#ffc107", "#17a2b8", "#dc3545", "#fd7e14", "#20c997"]
}
```

**SQL Query:**
```sql
SELECT
    Status,
    COUNT(*) AS Total,
    (CAST(COUNT(*) AS FLOAT) / (SELECT COUNT(*) FROM TrainingRequests WHERE YEAR(StartDate) = @Year)) * 100 AS Percentage
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
GROUP BY Status
ORDER BY Total DESC
```

**Interactive Features:**
- ✅ Click Status → Filter table and other charts
- ✅ Hover → Show count and percentage
- ✅ Animation on load

---

### **Chart 3: Trend รายเดือน (Line Chart)**

**Type:** Multi-line Chart
**Interactive:** ✅ Click to Drill-down to Month
**Library:** Chart.js / ApexCharts

**Data Structure:**
```javascript
{
    categories: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
    series: [
        {
            name: "จำนวนคำขอ",
            data: [12, 15, 18, 14, 20, 17, 19, 22, 18, 21, 16, 14]
        },
        {
            name: "ค่าใช้จ่าย (แสนบาท)",
            data: [8.5, 10.2, 12.1, 9.8, 15.3, 11.7, 13.5, 16.2, 12.8, 14.9, 11.2, 9.8]
        }
    ]
}
```

**SQL Query:**
```sql
SELECT
    MONTH(StartDate) AS Month,
    DATENAME(MONTH, StartDate) AS MonthName,
    COUNT(*) AS TotalRequests,
    SUM(TotalCost) / 100000 AS TotalCostIn100K
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
GROUP BY MONTH(StartDate), DATENAME(MONTH, StartDate)
ORDER BY MONTH(StartDate)
```

**Interactive Features:**
- ✅ Click on point → Filter to that month
- ✅ Hover → Show exact values
- ✅ Zoom in/out
- ✅ Toggle series (show/hide)

---

### **Chart 4: Top 5 Departments (List/Table)**

**Type:** Ranked List with Progress Bars
**Interactive:** ✅ Click to Filter

**SQL Query:**
```sql
SELECT TOP 5
    Department,
    COUNT(*) AS TotalRequests,
    SUM(TotalCost) AS TotalCost,
    AVG(TotalCost) AS AvgCost
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
  AND Status IN ('APPROVED', 'COMPLETE')
GROUP BY Department
ORDER BY TotalCost DESC
```

---

### **Chart 5: Processing Time Analysis (Gauge/Metric)**

**Type:** Metric Cards with Gauges
**Purpose:** วิเคราะห์ความเร็วในการอนุมัติ

**SQL Query:**
```sql
-- Average processing time by status transition
SELECT
    AVG(DATEDIFF(DAY, CreatedDate,
        CASE
            WHEN Status = 'APPROVED' THEN ModifiedDate
            ELSE GETDATE()
        END)) AS AvgDays
FROM TrainingRequests
WHERE YEAR(StartDate) = @Year
```

---

## 🎯 Interactive Features (Power BI Style)

### **1. Cross-Filtering**
```javascript
// เมื่อ Click ที่ Bar Chart (เลือก Department "IT")
filterAllCharts({
    department: "IT"
});

// ผลลัพธ์:
// - Donut Chart → แสดงเฉพาะ Status ของ IT
// - Line Chart → แสดงเฉพาะ Trend ของ IT
// - Table → แสดงเฉพาะรายการของ IT
// - KPIs → คำนวณใหม่เฉพาะ IT
```

### **2. Date Range Filter**
```javascript
// Admin เลือก Date Range: 01/01/2025 - 30/06/2025
applyDateFilter({
    startDate: "2025-01-01",
    endDate: "2025-06-30"
});

// ทุก Chart และ KPI จะ Refresh ข้อมูลตามช่วงเวลา
```

### **3. Drill-down**
```javascript
// Click ที่ Donut Chart (Status "Approved")
drillDown({
    status: "Approved",
    view: "detail"
});

// แสดง Modal หรือ Navigate ไปหน้า Detail
// พร้อม Filter เฉพาะ Status "Approved"
```

### **4. Reset Filters**
```html
<button onclick="resetAllFilters()">
    🔄 Reset All Filters
</button>
```

---

## 🗄️ API Endpoints Design

### **1. GET /api/Dashboard/Summary**
**Purpose:** ข้อมูล KPI Cards

**Parameters:**
- `year` (int): ปี (default: ปีปัจจุบัน)
- `startDate` (datetime): วันที่เริ่มต้น
- `endDate` (datetime): วันที่สิ้นสุด
- `department` (string): ฝ่าย (optional)

**Response:**
```json
{
    "success": true,
    "data": {
        "totalCost": 2548320,
        "totalCostTrend": 12.3,
        "budgetUsagePercent": 68.5,
        "totalQuota": 3000000,
        "totalRequests": 156,
        "totalRequestsTrend": 23,
        "approvalRate": 78.2,
        "approvalRateTrend": 5.1
    }
}
```

---

### **2. GET /api/Dashboard/CostByDepartment**
**Purpose:** งบประมาณแต่ละฝ่าย

**Response:**
```json
{
    "success": true,
    "data": [
        {
            "department": "IT",
            "totalUsed": 850000,
            "quota": 1000000,
            "remaining": 150000,
            "usagePercent": 85.0
        },
        ...
    ]
}
```

---

### **3. GET /api/Dashboard/StatusDistribution**
**Purpose:** จำนวนเอกสารแต่ละ Status

**Response:**
```json
{
    "success": true,
    "data": [
        {
            "status": "Approved",
            "count": 70,
            "percentage": 44.87
        },
        ...
    ]
}
```

---

### **4. GET /api/Dashboard/MonthlyTrend**
**Purpose:** Trend รายเดือน

**Response:**
```json
{
    "success": true,
    "data": {
        "months": ["Jan", "Feb", "Mar", ...],
        "requests": [12, 15, 18, ...],
        "costs": [850000, 1020000, 1210000, ...]
    }
}
```

---

### **5. GET /api/Dashboard/TopDepartments**
**Purpose:** Top 5 Departments

**Response:**
```json
{
    "success": true,
    "data": [
        {
            "department": "IT",
            "totalRequests": 45,
            "totalCost": 850000,
            "avgCost": 18889
        },
        ...
    ]
}
```

---

### **6. GET /api/Dashboard/FilteredRequests**
**Purpose:** รายการเอกสารที่ถูก Filter

**Parameters:**
- `year`, `startDate`, `endDate`, `department`, `status`
- `pageNumber`, `pageSize` (for pagination)

**Response:**
```json
{
    "success": true,
    "data": [
        {
            "docNo": "TR-2025-001",
            "department": "IT",
            "seminarTitle": "Azure Training",
            "totalCost": 50000,
            "status": "Approved",
            "startDate": "2025-11-15"
        },
        ...
    ],
    "pagination": {
        "currentPage": 1,
        "totalPages": 5,
        "totalRecords": 48
    }
}
```

---

## 🎨 Technology Stack

### **Frontend:**
- **Charts:** Chart.js (lightweight) หรือ ApexCharts (advanced features)
- **UI Framework:** Bootstrap 5 (existing)
- **Icons:** Font Awesome 6
- **AJAX:** Fetch API / Axios
- **Date Picker:** Flatpickr

### **Backend:**
- **.NET Core MVC** (existing)
- **SQL Server** (existing)
- **JSON Response** for API

### **Performance:**
- **Caching:** MemoryCache for Dashboard data (30 sec - 1 min)
- **Lazy Loading:** Charts load on scroll
- **Debouncing:** Date filter apply after 500ms

---

## 🚀 Implementation Phases

### **Phase 1: Backend API (Day 1-2)**
1. ✅ Create HomeController API endpoints
2. ✅ SQL queries optimization
3. ✅ Add caching layer
4. ✅ Testing with Postman

### **Phase 2: Frontend UI (Day 3-4)**
1. ✅ Update Index.cshtml layout
2. ✅ Integrate Chart.js/ApexCharts
3. ✅ Add KPI cards with real data
4. ✅ Implement date range filter

### **Phase 3: Interactivity (Day 5)**
1. ✅ Cross-filtering logic
2. ✅ Click events on charts
3. ✅ Drill-down modal
4. ✅ Filter state management

### **Phase 4: Testing & Optimization (Day 6)**
1. ✅ Performance testing
2. ✅ Browser compatibility
3. ✅ Responsive design
4. ✅ Security testing

---

## 💡 Additional Features (Nice to Have)

### **1. Export to Excel**
- Export filtered data to Excel file
- Include charts as images

### **2. Scheduled Reports**
- Email daily/weekly summary to Admin
- Auto-generated PDF reports

### **3. Predictive Analytics**
- Forecast budget usage for next quarter
- Predict approval time based on historical data

### **4. Mobile Responsive**
- Swipe gestures for charts
- Collapsible sections

### **5. Dark Mode**
- Toggle between light/dark theme
- Save preference in session

---

## ⚠️ ข้อควรระวัง (ไม่กระทบโครงสร้างเดิม)

### ✅ **สิ่งที่จะทำ:**
1. สร้าง API endpoints ใหม่ใน `HomeController.cs`
2. อัพเดท `Views/Home/Index.cshtml` เท่านั้น
3. เพิ่ม JavaScript สำหรับ Interactivity
4. ใช้ CSS แยกไฟล์ใหม่

### ❌ **สิ่งที่จะไม่ทำ:**
1. ไม่แก้ไข Database Schema
2. ไม่แก้ไข Controllers อื่นๆ
3. ไม่แก้ไข Views อื่นๆ
4. ไม่แก้ไข Authentication/Authorization logic

---

## 📊 Success Metrics

1. **Load Time:** < 2 seconds (first load)
2. **Interactivity:** < 100ms (chart updates)
3. **Accuracy:** 100% (ข้อมูลตรงกับฐานข้อมูล)
4. **Responsive:** รองรับ Desktop, Tablet, Mobile
5. **Browser Support:** Chrome, Edge, Firefox, Safari (latest)

---

## 🎯 สรุป

Dashboard นี้จะช่วยให้:
- ✅ Admin วิเคราะห์ข้อมูลได้อย่างรวดเร็ว
- ✅ เห็นภาพรวมงบประมาณทันที
- ✅ ติดตามสถานะเอกสารแบบ Real-time
- ✅ ตัดสินใจได้อย่างมีข้อมูลสนับสนุน
- ✅ ประหยัดเวลาในการหาข้อมูล

**พร้อม Implement ทันที!** 🚀
