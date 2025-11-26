# 🔐 Session Management & Auto-Redirect to Login

## 📋 Overview

ระบบตรวจสอบ Session โดยอัตโนมัติ เมื่อ User เข้าถึงหน้าที่ต้องการ Authentication แต่ไม่มี Session (เช่น Session หมดอายุ) ระบบจะ Redirect ไป Login พร้อมบันทึก URL ที่ต้องการกลับไป หลัง Login สำเร็จจะกลับไปที่หน้าเดิมโดยอัตโนมัติ

---

## 🎯 Problem & Solution

### ❌ ปัญหาเดิม
- User คลิก Link จาก Email แต่ Session หมดอายุ
- ระบบแสดงหน้า View อย่างเดียว ไม่มีการแจ้งเตือน
- User ต้อง Login แล้วค้นหาเอกสารใหม่

### ✅ วิธีแก้ไข
- ตรวจจับว่าไม่มี Session → Redirect ไป Login ทันที
- บันทึก ReturnUrl (URL ที่ต้องการกลับไป)
- หลัง Login สำเร็จ → Redirect กลับไปที่หน้าเดิมโดยอัตโนมัติ

---

## 🏗️ Architecture

### 1. **RequireSessionAttribute** (Action Filter)
**ไฟล์:** `Filters/RequireSessionAttribute.cs`

```csharp
[RequireSession(LoginRoute = "/Login/Index", Message = "กรุณาล็อกอินเพื่อดำเนินการต่อ")]
public async Task<IActionResult> Edit(string docNo)
{
    // ...
}
```

**การทำงาน:**
1. เช็คว่ามี `HttpContext.Session.GetString("UserEmail")` หรือไม่
2. ถ้าไม่มี → บันทึก ReturnUrl ใน TempData
3. Redirect ไป Login Page

### 2. **LoginController** (รองรับ ReturnUrl)
**ไฟล์:** `Controllers/LoginController.cs`

```csharp
[HttpGet]
public IActionResult Index(string returnUrl = null)
{
    ViewBag.ReturnUrl = returnUrl;
    // แสดงข้อความ Info จาก TempData
    return View("Login");
}

[HttpPost]
public IActionResult Authenticate(LoginViewModel model, string returnUrl = null)
{
    // หลัง Login สำเร็จ
    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    {
        return Redirect(returnUrl); // กลับไปหน้าเดิม
    }
    return RedirectToAction("Index", "Home"); // หน้า Default
}
```

### 3. **TrainingRequestController** (ใช้ Filter)
**ไฟล์:** `Controllers/TrainingRequestController.cs`

```csharp
using TrainingRequestApp.Filters;

[HttpGet]
[RequireSession(LoginRoute = "/Login/Index", Message = "กรุณาล็อกอินเพื่อดูรายละเอียดเอกสาร")]
public async Task<IActionResult> Edit(string docNo)
{
    // ...
}

[HttpGet]
[RequireSession(LoginRoute = "/Login/Index", Message = "กรุณาล็อกอินเพื่อดู Approval Flow")]
public async Task<IActionResult> ApprovalFlow(string docNo)
{
    // ...
}
```

---

## 🔄 Flow Diagram

```
User คลิก Email Link
    ↓
GET /TrainingRequest/Edit?docNo=PB-2025-01-001
    ↓
[RequireSession] Filter ทำงาน
    ↓
    ├─ มี Session → ผ่าน → แสดงหน้า Edit
    │
    └─ ไม่มี Session
           ↓
       บันทึก TempData:
       - ReturnUrl = "/TrainingRequest/Edit?docNo=PB-2025-01-001"
       - Info = "กรุณาล็อกอินเพื่อดูรายละเอียดเอกสาร"
           ↓
       Redirect ไป: /Login/Index?returnUrl=/TrainingRequest/Edit?docNo=PB-2025-01-001
           ↓
       แสดงหน้า Login พร้อมข้อความ Info
           ↓
       User ใส่ Username/Password
           ↓
       POST /Login/Authenticate
           ↓
       ตรวจสอบ Credentials
           ↓
           ├─ ถูกต้อง
           │    ↓
           │  บันทึก Session:
           │  - UserEmail
           │  - UserRole
           │  - UserId
           │  - Company
           │    ↓
           │  เช็ค returnUrl
           │    ↓
           │    ├─ มี returnUrl → Redirect ไป /TrainingRequest/Edit?docNo=PB-2025-01-001
           │    └─ ไม่มี returnUrl → Redirect ไป /Home/Index
           │
           └─ ไม่ถูกต้อง → แสดง Error Message
```

---

## 🎨 User Experience

### Scenario 1: Session หมดอายุ

**ก่อนแก้ไข:**
```
User คลิก Email Link
  → เห็นหน้า View (ไม่มีปุ่ม Approve)
  → งง ไม่รู้ว่าทำไม
  → ต้องไป Login เอง
  → ค้นหาเอกสารใหม่
```

**หลังแก้ไข:**
```
User คลิก Email Link
  → เห็นหน้า Login พร้อมข้อความ "กรุณาล็อกอินเพื่อดูรายละเอียดเอกสาร"
  → Login
  → กลับมาที่หน้าเอกสารทันที (พร้อมปุ่ม Approve ถ้ามีสิทธิ์)
```

### Scenario 2: Login ด้วย Email ผิด

**ก่อนแก้ไข:**
```
User คลิก Email Link (Email: manager@company.com)
  → Login ด้วย: employee@company.com
  → เห็นหน้า View (ไม่มีปุ่ม Approve)
  → งง ไม่รู้ว่าทำไม
```

**หลังแก้ไข:**
```
User คลิก Email Link (Email: manager@company.com)
  → Login ด้วย: employee@company.com
  → เห็นหน้า View พร้อม Warning:
      "⚠️ คุณไม่มีสิทธิ์อนุมัติเอกสารนี้
       คุณ Login ด้วย Email: employee@company.com
       กรุณาตรวจสอบว่าคุณ Login ด้วย Email ที่ถูกต้องหรือไม่"
  → เข้าใจปัญหาทันที → Logout → Login ใหม่
```

---

## 🔧 Configuration Options

### RequireSessionAttribute Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SessionKey` | string | `"UserEmail"` | Session Key ที่ใช้เช็ค |
| `LoginRoute` | string | `"/Account/Login"` | Login URL |
| `Message` | string | `"กรุณาล็อกอินเพื่อดำเนินการต่อ"` | ข้อความที่แสดง |

### ตัวอย่างการใช้งาน Custom

```csharp
// ใช้ Default Settings
[RequireSession]
public IActionResult MyAction() { }

// Custom Login Route
[RequireSession(LoginRoute = "/Login/Index")]
public IActionResult MyAction() { }

// Custom Message
[RequireSession(Message = "คุณต้อง Login ก่อนถึงจะเข้าถึงหน้านี้ได้")]
public IActionResult MyAction() { }

// Custom ทั้งหมด
[RequireSession(
    SessionKey = "UserEmail",
    LoginRoute = "/Auth/Login",
    Message = "Session expired. Please login again."
)]
public IActionResult MyAction() { }
```

---

## 🛡️ Security Features

### 1. **Local URL Check**
```csharp
if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
{
    return Redirect(returnUrl);
}
```
**ป้องกัน:** Open Redirect Attack (ไม่ให้ Redirect ไปเว็บภายนอก)

### 2. **TempData (Self-Destructing)**
```csharp
TempData["ReturnUrl"] = returnUrl.ToString();
TempData["Info"] = Message;
```
**ป้องกัน:** TempData หายหลังใช้งาน 1 ครั้ง → ป้องกันการใช้ซ้ำ

### 3. **Session-based Authentication**
- ไม่ใช้ Token ใน URL → ป้องกัน Token Leakage
- ต้อง Login ด้วย Email ที่ถูกต้อง → Double-Check ที่ UI + Action Level

---

## 📊 Monitoring & Logging

### Console Output

**เมื่อไม่มี Session:**
```
⚠️ RequireSession: No session found
   SessionKey: UserEmail
   ReturnUrl: /TrainingRequest/Edit?docNo=PB-2025-01-001
   Redirecting to: /Login/Index
```

**เมื่อมี Session:**
```
✅ RequireSession: Session found - UserEmail: manager@company.com
```

**Login Success:**
```
🟢 Login Successful: 1234567 (manager@company.com)
🔄 Redirecting to ReturnUrl: /TrainingRequest/Edit?docNo=PB-2025-01-001
```

---

## 🚀 Implementation Checklist

- ✅ สร้าง `RequireSessionAttribute` Filter
- ✅ แก้ไข `LoginController.Index()` รองรับ ReturnUrl
- ✅ แก้ไข `LoginController.Authenticate()` Redirect กลับ ReturnUrl
- ✅ เพิ่ม `using TrainingRequestApp.Filters;` ใน TrainingRequestController
- ✅ ใส่ `[RequireSession]` ที่ `Edit()` Action
- ✅ ใส่ `[RequireSession]` ที่ `ApprovalFlow()` Action
- ⏭️ (Optional) แสดง Warning Message ใน Edit View เมื่อไม่มีสิทธิ์
- ⏭️ (Optional) แก้ไข Login View เพื่อรองรับ ReturnUrl ใน Form

---

## 📝 Additional Notes

### การทำงานของ TempData

**TempData:**
- เก็บข้อมูลระหว่าง Requests (แต่ใช้ได้แค่ 1 ครั้ง)
- เหมาะสำหรับ Redirect Scenarios
- หายหลัง Render 1 ครั้ง

**ตัวอย่าง:**
```csharp
// Controller 1
TempData["Message"] = "Hello";
return RedirectToAction("Index");

// Controller 2
var msg = TempData["Message"]; // "Hello"
var msg2 = TempData["Message"]; // null (หายแล้ว)
```

### ReturnUrl Best Practices

**✅ ควร:**
- ใช้ `Url.IsLocalUrl()` เสมอ (ป้องกัน Open Redirect)
- URL Encode ReturnUrl
- จำกัดความยาว ReturnUrl

**❌ ไม่ควร:**
- ยอมรับ External URLs
- ใส่ Token/Password ใน ReturnUrl
- Redirect โดยไม่เช็ค URL Validity

---

## 🎓 Conclusion

ระบบ Auto-Redirect to Login นี้:
- ✅ ไม่กระทบโครงสร้างเดิม (เพิ่ม Filter เท่านั้น)
- ✅ ใช้งานง่าย (ใส่ Attribute เดียว)
- ✅ Reusable (ใช้ได้กับทุก Action)
- ✅ ปลอดภัย (มี Security Checks)
- ✅ User-Friendly (แจ้งเตือนชัดเจน + Auto-Redirect)

**ผลลัพธ์:**
- User ไม่สับสนเมื่อ Session หมดอายุ
- ลด Support Tickets
- ประสบการณ์การใช้งานดีขึ้น
- ระบบมีความปลอดภัยมากขึ้น
