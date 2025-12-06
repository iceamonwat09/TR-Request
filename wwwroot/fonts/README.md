# 📁 Thai Fonts สำหรับ PDF Export

## วิธีติดตั้ง Thai Font (ถ้าต้องการใช้ THSarabunNew)

---

## 🎯 ขั้นตอนการติดตั้ง THSarabunNew Font

### **1. ดาวน์โหลด Font**

เลือกดาวน์โหลดจากแหล่งใดแหล่งหนึ่ง:

**ตัวเลือก A: จาก Google Fonts (แนะนำ)**
- [Sarabun Font](https://fonts.google.com/specimen/Sarabun)
- คลิก "Download family"
- Extract ไฟล์ `.ttf` ออกมา

**ตัวเลือก B: THSarabunNew (มาตรฐานราชการ)**
- [F0nt.com - THSarabunNew](https://www.f0nt.com/release/thsarabunnew/)
- [CadSoftTools](https://www.cadsofttools.com/download/THSarabun.zip)
- Download และ Extract

**ตัวเลือก C: จาก GitHub**
- [THSarabunNew Repository](https://github.com/fontuni/thsarabunnew)

---

### **2. Copy Font Files ลงโฟลเดอร์นี้**

Copy ไฟล์ font ทั้งหมดลงในโฟลเดอร์นี้ (`wwwroot/fonts/`)

**ไฟล์ที่ต้องการ:**
```
wwwroot/fonts/
├── THSarabunNew.ttf           (Regular)
├── THSarabunNew-Bold.ttf      (Bold)
├── THSarabunNew-Italic.ttf    (Italic)
└── THSarabunNew-BoldItalic.ttf (Bold Italic)
```

หรือถ้าใช้ Sarabun จาก Google Fonts:
```
wwwroot/fonts/
├── Sarabun-Regular.ttf
├── Sarabun-Bold.ttf
└── Sarabun-Medium.ttf
```

---

### **3. แก้ไข PdfReportService.cs**

**ปัจจุบันใช้:** `Tahoma` (Windows default font)

**ถ้าต้องการใช้ THSarabunNew:** แก้ไขที่ไฟล์ `/Services/PdfReportService.cs`

```csharp
// เพิ่ม using
using System.IO;

// ในส่วน constructor, แก้บรรทัดที่ 36
string fontName = "Tahoma"; // ← แก้ตรงนี้

// เป็น
string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew.ttf");

// แล้วเปลี่ยน font name
string fontName = "THSarabunNew";
```

**หรือถ้าใช้ Sarabun จาก Google Fonts:**
```csharp
string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "Sarabun-Regular.ttf");
string fontName = "Sarabun";
```

---

## ⚡ Quick Start (ไม่ต้องใช้ Custom Font)

**ถ้าไม่อยากวุ่นวาย ใช้ Font ที่มีใน Windows อยู่แล้ว:**

ปัจจุบันระบบใช้ `Tahoma` ซึ่งรองรับภาษาไทยอยู่แล้ว ✅

**Font ที่รองรับภาษาไทยแบบ Built-in:**
- ✅ `Tahoma` (กำลังใช้อยู่)
- ✅ `Microsoft Sans Serif`
- ✅ `Segoe UI`
- ✅ `Cordia New`
- ✅ `Angsana New`

**วิธีเปลี่ยน Font:** แก้ไขที่ `/Services/PdfReportService.cs` บรรทัดที่ 36:

```csharp
string fontName = "Tahoma"; // เปลี่ยนเป็น "Microsoft Sans Serif", "Segoe UI", etc.
```

---

## 🔧 Troubleshooting

### ปัญหา 1: Font ไม่แสดงภาษาไทย (แสดงเป็นสี่เหลี่ยม)

**สาเหตุ:** Font ไม่รองรับ Unicode

**แก้ไข:** ตรวจสอบว่ามี `XPdfFontOptions(PdfFontEncoding.Unicode)` ใน constructor แล้วหรือยัง

```csharp
var options = new XPdfFontOptions(PdfFontEncoding.Unicode);
_fontNormal = new XFont(fontName, 10, XFontStyle.Regular, options);
```

### ปัญหา 2: Font file not found

**สาเหตุ:** Path ไม่ถูกต้อง

**แก้ไข:**
```csharp
// Debug: แสดง path ที่ค้นหา
var fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "THSarabunNew.ttf");
Console.WriteLine($"Looking for font at: {fontPath}");
```

### ปัญหา 3: Font ไม่สวย

**แก้ไข:** เพิ่มขนาด font

```csharp
_fontTitle = new XFont(fontName, 20, XFontStyle.Bold, options);  // เพิ่มจาก 16 → 20
_fontNormal = new XFont(fontName, 12, XFontStyle.Regular, options); // เพิ่มจาก 10 → 12
```

---

## 📚 Font Recommendations สำหรับภาษาไทย

| Font | ขนาดแนะนำ | เหมาะกับ | ติดตั้ง |
|------|-----------|----------|---------|
| **Tahoma** ⭐ | 10-12pt | เอกสารทั่วไป | Built-in Windows |
| **THSarabunNew** | 16-18pt | เอกสารราชการ | ต้อง download |
| **Sarabun** | 12-14pt | เอกสารสมัยใหม่ | Google Fonts |
| **Microsoft Sans Serif** | 10-12pt | เอกสารทั่วไป | Built-in Windows |

---

## ✅ สรุป

**ปัจจุบัน:** ระบบใช้ **Tahoma** อยู่แล้ว ซึ่งรองรับภาษาไทย ✅

**ถ้าต้องการเปลี่ยน:**
1. Download font → Copy ลงโฟลเดอร์นี้
2. แก้ไข `fontName` ใน `PdfReportService.cs`
3. Build + Run ใหม่

**ไม่แน่ใจ?** ใช้ Tahoma ต่อไปก่อน ทดสอบดู ถ้าไม่สวยค่อยเปลี่ยน!
