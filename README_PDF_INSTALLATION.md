# 📄 PDF Export Feature - Installation Guide

## คู่มือการติดตั้ง PdfSharpCore สำหรับ PDF Export

---

## 🎯 ภาพรวม

Feature นี้เพิ่มความสามารถในการ Export รายงานคำขอฝึกอบรม (Training Request Form) เป็น PDF โดยใช้ **PdfSharpCore Library**

### ✅ ข้อมูลที่รวมอยู่ใน PDF:
- ข้อมูลเอกสาร (เลขที่เอกสาร, บริษัท, แผนก)
- ข้อมูลหลักสูตร (ชื่อหลักสูตร, สถานที่, วิทยากร, วันที่)
- รายละเอียดงบประมาณ (แยกตามประเภท + รวมทั้งหมด)
- วัตถุประสงค์และผลที่คาดหวัง
- **ผู้อนุมัติ 5 ระดับ (แสดงเฉพาะที่ Status = APPROVED)**

---

## 📦 การติดตั้ง PdfSharpCore

### **ขั้นตอนที่ 1: ติดตั้ง NuGet Package**

เปิด Terminal ที่ root directory ของโปรเจค แล้วรันคำสั่ง:

```bash
dotnet add package PdfSharpCore --version 1.3.65
```

หรือ ถ้าใช้ Visual Studio:
1. Right-click โปรเจค → `Manage NuGet Packages`
2. ค้นหา `PdfSharpCore`
3. คลิก Install

---

### **ขั้นตอนที่ 2: Verify การติดตั้ง**

ตรวจสอบว่า PdfSharpCore ถูกติดตั้งแล้วโดยเช็คไฟล์:
- ถ้ามี `.csproj` file: เปิดดูแล้วต้องมี:
  ```xml
  <ItemGroup>
    <PackageReference Include="PdfSharpCore" Version="1.3.65" />
  </ItemGroup>
  ```

- หรือรัน:
  ```bash
  dotnet list package
  ```

---

### **ขั้นตอนที่ 3: Build โปรเจค**

```bash
dotnet build
```

ต้องไม่มี error เกี่ยวกับ PdfSharpCore

---

## 🚀 การใช้งาน

### **1. ผ่านหน้า Monthly Requests**

1. เข้าหน้า `Home/MonthlyRequests`
2. คลิกปุ่ม **รายงาน** (สีฟ้า, ไอคอน file-alt)
3. ระบบจะ download PDF ทันที

### **2. เรียกผ่าน URL โดยตรง**

```
GET /Home/ExportTrainingRequestPdf?id={TrainingRequestId}
```

**ตัวอย่าง:**
```
https://localhost:1253/Home/ExportTrainingRequestPdf?id=5
```

---

## 📁 ไฟล์ที่ถูกสร้าง/แก้ไข

### **ไฟล์ใหม่ (New Files):**
```
✅ /Services/IPdfReportService.cs        - Interface
✅ /Services/PdfReportService.cs         - Service หลักสำหรับสร้าง PDF
✅ /README_PDF_INSTALLATION.md           - เอกสารนี้
```

### **ไฟล์ที่แก้ไข (Modified Files):**
```
📝 /Controllers/HomeController.cs        - เพิ่ม method ExportTrainingRequestPdf()
📝 /Program.cs                           - register IPdfReportService
📝 /Views/Home/MonthlyRequests.cshtml   - แก้ function viewReport() (1 บรรทัด)
```

### **✅ ไม่กระทบระบบเดิม:**
- ไม่แก้ไข Database Schema
- ไม่แก้ไข Model classes
- ไม่แก้ไข View อื่นๆ
- ไม่แก้ไข Controller methods เดิม

---

## 🔧 การทำงานของระบบ

### **1. User Flow:**
```
User คลิกปุ่ม "รายงาน"
    ↓
JavaScript: viewReport(id)
    ↓
Redirect → /Home/ExportTrainingRequestPdf?id=X
    ↓
HomeController.ExportTrainingRequestPdf(id)
    ↓
PdfReportService.GenerateTrainingRequestPdfAsync(id)
    ↓
ดึงข้อมูลจาก Database (TrainingRequests table)
    ↓
สร้าง PDF ด้วย PdfSharpCore
    ↓
Return PDF file (download)
```

### **2. PDF Layout:**
```
┌─────────────────────────────────────────┐
│     แบบฟอร์มคำขอฝึกอบรม                 │
│     TRAINING REQUEST FORM                │
├─────────────────────────────────────────┤
│ [ข้อมูลเอกสาร]                          │
│  - เลขที่เอกสาร                         │
│  - บริษัท                               │
│  - ประเภทการอบรม                        │
│  - แผนก                                  │
├─────────────────────────────────────────┤
│ [ข้อมูลหลักสูตร]                        │
│  - ชื่อหลักสูตร                         │
│  - สถานที่อบรม                          │
│  - วันที่อบรม                           │
│  - จำนวนผู้เข้าอบรม                     │
├─────────────────────────────────────────┤
│ [รายละเอียดงบประมาณ]                   │
│  - ค่าลงทะเบียน                         │
│  - ค่าวิทยากร                           │
│  - ค่าอุปกรณ์                           │
│  - รวมทั้งหมด                           │
├─────────────────────────────────────────┤
│ [วัตถุประสงค์]                          │
│  - วัตถุประสงค์หลัก                     │
│  - ผลที่คาดหวัง                         │
├─────────────────────────────────────────┤
│ [ผู้อนุมัติ]                            │
│  1. Section Manager ✅ (ถ้า APPROVED)   │
│  2. Department Manager ✅               │
│  3. HRD Admin ✅                        │
│  4. HRD Confirmation ✅                 │
│  5. Managing Director ✅                │
├─────────────────────────────────────────┤
│ Footer: สถานะ | วันที่สร้าง            │
└─────────────────────────────────────────┘
```

---

## 📊 เงื่อนไขการแสดงผู้อนุมัติ

**สำคัญ:** ผู้อนุมัติจะแสดงชื่อและเวลาเฉพาะเมื่อ:

```csharp
if (Status == "APPROVED") {
    // แสดง ชื่อผู้อนุมัติ + วันที่อนุมัติ
} else {
    // แสดง "สถานะ: รออนุมัติ"
}
```

**ตัวอย่าง:**
- `Status_SectionManager = "APPROVED"` → แสดง "ผู้อนุมัติ: john@company.com, วันที่อนุมัติ: 2025-12-06 14:30"
- `Status_SectionManager = null` → แสดง "สถานะ: รออนุมัติ"

---

## ⚙️ Configuration

### **Database Connection:**
ใช้ `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=HRDSYSTEM;..."
  }
}
```

### **Font Support:**
- ใช้ `Arial` font (รองรับภาษาไทย)
- ถ้าต้องการเปลี่ยน font: แก้ที่ `PdfReportService.cs` (constructor)

---

## 🐛 Troubleshooting

### **ปัญหา 1: Cannot find PdfSharpCore**
```
Error: The type or namespace name 'PdfSharpCore' could not be found
```

**แก้ไข:**
```bash
dotnet add package PdfSharpCore --version 1.3.65
dotnet restore
dotnet build
```

### **ปัญหา 2: Service not registered**
```
Error: Unable to resolve service for type 'IPdfReportService'
```

**แก้ไข:** เช็คว่า `Program.cs` มีบรรทัดนี้:
```csharp
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
```

### **ปัญหา 3: Training Request not found**
```
Error: Training Request ID X not found
```

**แก้ไข:** เช็คว่า:
- ID มีอยู่ในตาราง `TrainingRequests`
- `IsActive = 1`

### **ปัญหา 4: Font ภาษาไทยไม่แสดง**
**แก้ไข:** ใช้ font ที่รองรับ Unicode เช่น `Arial`, `Tahoma`, `Sarabun`

---

## 🔒 Security & Performance

### **Security:**
- ✅ ตรวจสอบ Session ก่อนใช้งาน
- ✅ ใช้ Parameterized SQL queries (ป้องกัน SQL Injection)
- ✅ ไม่ส่งข้อมูล sensitive ออกนอก server
- ✅ On-Premise processing (ไม่ต้องใช้เน็ต)

### **Performance:**
- ✅ Async/Await pattern
- ✅ MemoryStream สำหรับ PDF generation
- ✅ Single database query
- ⚡ เร็ว (~1-2 วินาที per PDF)

---

## 📞 Support & Further Development

### **การพัฒนาเพิ่มเติม:**
1. เพิ่มรายชื่อผู้เข้าอบรม (TrainingRequestEmployees table)
2. เพิ่ม Company Logo
3. เพิ่ม Barcode/QR Code
4. Export หลายรายการพร้อมกัน (Batch Export)
5. Email PDF แทนการ download

### **การแก้ไข Layout:**
แก้ไขที่ไฟล์: `/Services/PdfReportService.cs`
- Method `GenerateTrainingRequestPdfAsync()`
- Helper methods: `DrawSectionHeader()`, `DrawLabelValue()`, etc.

---

## ✅ Checklist การ Deploy

- [ ] ติดตั้ง PdfSharpCore package
- [ ] Build โปรเจคสำเร็จ
- [ ] Test บน Development environment
- [ ] Verify PDF สามารถ download ได้
- [ ] เช็คภาษาไทยใน PDF
- [ ] เช็คผู้อนุมัติแสดงถูกต้อง (เฉพาะ APPROVED)
- [ ] Test performance (ระยะเวลา generate PDF)
- [ ] Deploy to Production
- [ ] Test บน Production environment

---

## 📅 Version History

| วันที่ | เวอร์ชัน | การเปลี่ยนแปลง |
|--------|----------|----------------|
| 2025-12-06 | 1.0 | Initial release - PDF Export Feature with PdfSharpCore |

---

**🎉 Feature พร้อมใช้งาน! ติดตั้ง PdfSharpCore แล้วทดสอบได้เลย**
