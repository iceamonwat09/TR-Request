using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// PDF Report Service - แบบฟอร์มคำขอฝึกอบรม (Training Request Form)
    ///
    /// Version: 5.7 (Sign Underline Full Width)
    /// - v5.5: เพิ่มเส้นใต้ให้ "การชำระเงิน", Checkbox ตรงกับ "เงินสด"
    /// - v5.6: แก้ไขเส้นใต้ 3 checkbox ให้สิ้นสุดที่ตำแหน่ง checkbox
    /// - v5.7: ปรับเส้นใต้ "ลงชื่อผู้ขออบรม:" ให้ยาวถึงขอบ
    /// </summary>
    public class PdfReportService : IPdfReportService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        // Fonts
        private XFont _fontTitle;
        private XFont _fontHeader;
        private XFont _fontNormal;
        private XFont _fontSmall;
        private XFont _fontBold;
        private XFont _fontTiny;

        // Colors & Pens
        private XSolidBrush _grayBrush;
        private XPen _borderPen;
        private XPen _thickPen;
        private XPen _thinPen;
        private XPen _dottedPen;

        // ป้องกันการลงทะเบียน FontResolver ซ้ำ (thread-safe)
        private static bool _fontResolverRegistered = false;
        private static readonly object _fontResolverLock = new object();

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // ลงทะเบียน ThaiFontResolver เพื่อฝังฟอนต์ .ttf ลง PDF
            // แก้ปัญหาไม้เอก ไม้โท สระบน ไม่แสดงบน Linux Server
            lock (_fontResolverLock)
            {
                if (!_fontResolverRegistered)
                {
                    GlobalFontSettings.FontResolver = new ThaiFontResolver();
                    _fontResolverRegistered = true;
                }
            }

            var options = new XPdfFontOptions(PdfFontEncoding.Unicode);
            string fontName = "Tahoma";

            _fontTitle = new XFont(fontName, 14, XFontStyle.Bold, options);
            _fontHeader = new XFont(fontName, 10, XFontStyle.Bold, options);
            _fontNormal = new XFont(fontName, 9, XFontStyle.Regular, options);
            _fontSmall = new XFont(fontName, 8, XFontStyle.Regular, options);
            _fontBold = new XFont(fontName, 9, XFontStyle.Bold, options);
            _fontTiny = new XFont(fontName, 7, XFontStyle.Regular, options);

            _grayBrush = new XSolidBrush(XColor.FromArgb(217, 217, 217));
            _borderPen = new XPen(XColors.Black, 0.75);
            _thickPen = new XPen(XColors.Black, 1.5);
            _thinPen = new XPen(XColors.Black, 0.5);
            _dottedPen = new XPen(XColors.Black, 0.5) { DashStyle = XDashStyle.Dot };
        }

        public async Task<byte[]> GenerateTrainingRequestPdfAsync(int trainingRequestId)
        {
            try
            {
                var data = await GetTrainingRequestDataAsync(trainingRequestId);
                if (data == null)
                    throw new Exception($"Training Request ID {trainingRequestId} not found");

                PdfDocument document = new PdfDocument();
                document.Info.Title = $"Training Request - {data.DocNo}";

                PdfPage page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;

                XGraphics gfx = XGraphics.FromPdfPage(page);

                double margin = 25;
                double yPos = margin;
                double pageWidth = page.Width - (2 * margin);
                double vlabelWidth = 22;

                // === Header 3 ช่อง ===
                double headerHeight = DrawHeader(gfx, data, margin, yPos, pageWidth);
                yPos += headerHeight;

                // ส่วนที่ 1
                double section1Height = DrawSection1(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section1Height;

                // ส่วนที่ 2
                double section2Height = DrawSection2(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section2Height;

                // ส่วนที่ 3 (ซ้าย) & ส่วนที่ 4 (ขวา) - อยู่เคียงข้างกัน
                double section3And4Height = DrawSection3And4(gfx, data, margin, yPos, pageWidth, vlabelWidth);

                using (var stream = new System.IO.MemoryStream())
                {
                    document.Save(stream, false);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF: {ex.Message}");
                throw;
            }
        }

        // ==========================================
        // Header: 3 ช่อง (TIPAK | ชื่อฟอร์ม | DocNo)
        // ==========================================
        private double DrawHeader(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double headerHeight = 35;
            double colWidth = width / 3;

            // วาดกรอบ Header
            gfx.DrawRectangle(_thickPen, x, y, width, headerHeight);

            // เส้นแบ่งคอลัมน์
            gfx.DrawLine(_borderPen, x + colWidth, y, x + colWidth, y + headerHeight);
            gfx.DrawLine(_borderPen, x + (colWidth * 2), y, x + (colWidth * 2), y + headerHeight);

            // ช่องซ้าย: TIPAK
            var tipakFont = new XFont("Tahoma", 14, XFontStyle.Bold);
            DrawThaiString(gfx,"TIPAK", tipakFont, XBrushes.Black,
                new XRect(x, y, colWidth, headerHeight), XStringFormats.Center);

            // ช่องกลาง: ชื่อฟอร์ม
            DrawThaiString(gfx,"ใบขออบรมสัมมนาภายใน-ภายนอก", _fontBold,
                XBrushes.Black, new XRect(x + colWidth, y + 8, colWidth, 14), XStringFormats.Center);
            DrawThaiString(gfx,"(Inhouse/Public Training Request)", _fontSmall,
                XBrushes.Black, new XRect(x + colWidth, y + 20, colWidth, 12), XStringFormats.Center);

            // ช่องขวา: DocNo (กึ่งกลางทั้งช่อง)
            // [FIX v4.5] ลบ "ลำดับที่ใบขอ (เฉพาะ HR)" และจัด DocNo กึ่งกลาง
            DrawThaiString(gfx,data.DocNo ?? "", _fontBold,
                XBrushes.Black, new XRect(x + (colWidth * 2), y, colWidth, headerHeight), XStringFormats.Center);

            return headerHeight;
        }

        // ==========================================
        // ส่วนที่ 1: ผู้ขอฝึกอบรมกรอก
        // ==========================================
        private double DrawSection1(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;
            double rowHeight = 16;  // [FIX v4.1] ลดจาก 18 → 16
            double padding = 5;
            double textOffsetY = 11; // [FIX v4.1] ลดจาก 12 → 11

            // [FIX v4.1] เพิ่มจาก 444 → 462 (Option B) + ลด rowHeight (Option C)
            double sectionHeight = 462;

            DrawVerticalLabel(gfx, x, y, vlabelWidth, sectionHeight, "ส่วนที่ 1 ผู้ร้องขอกรอกข้อมูล");
            gfx.DrawRectangle(_thickPen, contentX, y, contentWidth, sectionHeight);

            currentY = y + padding + 2;

            // === Row 1: ประเภทการอบรม ===
            double labelX = contentX + padding;
            double checkboxCol1 = contentX + 100;
            double checkboxCol2 = contentX + 290;

            DrawThaiString(gfx,"ประเภทการอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            // [FIX v4.5] ปรับ checkbox offset จาก +4 → +2 เพื่อไม่ให้ด้านล่างถูกตัด
            DrawCheckbox(gfx, checkboxCol1, currentY + 2, data.TrainingType == "In House");
            DrawThaiString(gfx,"อบรมภายใน (In-House Training)", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 2, data.TrainingType == "Public");
            DrawThaiString(gfx,"อบรมภายนอก (Public Training)", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 2: สาขา (ใช้ Company แทน Factory) ===
            DrawThaiString(gfx,"สาขา :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            // [FIX v4.5] ปรับ checkbox offset จาก +4 → +2 เพื่อไม่ให้ด้านล่างถูกตัด
            DrawCheckbox(gfx, checkboxCol1, currentY + 2, data.Company?.Contains("สมุทรสาคร") == true);
            DrawThaiString(gfx,"สมุทรสาคร", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 2, data.Company?.Contains("ปราจีนบุรี") == true);
            DrawThaiString(gfx,"ปราจีนบุรี", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            currentY += rowHeight;

            // [FIX v4.2] ลบเส้นใต้สาขาออกเพื่อเพิ่มพื้นที่

            // === Row 3: เรียน / สำเนาเรียน ===
            double halfWidth = contentWidth / 2;
            DrawThaiString(gfx,"เรียน :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            // [FIX v5.4] ปรับเส้นให้ยาวถึง สำเนาเรียน โดยมีระยะห่างเหมาะสม
            DrawUnderlineText(gfx, labelX + 40, currentY + textOffsetY, halfWidth - 55, "ผู้จัดการฝ่ายทรัพยากรบุคคล", 3);

            double rightColX = contentX + halfWidth + padding;
            DrawThaiString(gfx,"สำเนาเรียน :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 68, currentY + textOffsetY + 3, halfWidth - 80);
            currentY += rowHeight;

            // === Row 4: ด้วยแผนก / ฝ่าย / มีความประสงค์จะ ===
            // [FIX v5.4] เพิ่ม : หลังหัวข้อ
            DrawThaiString(gfx,"ด้วยแผนก :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 62, currentY + textOffsetY, halfWidth - 75, data.Position ?? "", 3);

            // [FIX v4.3] ให้ "ฝ่าย" ตรงกับ "สำเนาเรียน :" ด้านบน
            DrawThaiString(gfx,"ฝ่าย :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderlineText(gfx, rightColX + 32, currentY + textOffsetY, 115, data.Department ?? "", 3);

            DrawThaiString(gfx,"มีความประสงค์จะ", _fontBold, XBrushes.Black, new XPoint(rightColX + 160, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 6: ขอฝึกอบรมหลักสูตร ===
            // [FIX v5.4] เพิ่ม space ก่อน : และปรับเส้นให้ยาวเต็ม
            DrawThaiString(gfx,"ขอฝึกอบรมหลักสูตร :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 110, currentY + textOffsetY, contentWidth - 120, data.SeminarTitle ?? "", 3);
            currentY += rowHeight;

            // === Row 7: วัน/เวลา & ระยะเวลา ===
            DrawThaiString(gfx,"วัน/เวลาที่จัดอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            string dateRange = "";
            if (data.StartDate.HasValue && data.EndDate.HasValue)
                dateRange = $"{data.StartDate.Value:dd/MM/yyyy} - {data.EndDate.Value:dd/MM/yyyy}";
            DrawUnderlineText(gfx, labelX + 105, currentY + textOffsetY, halfWidth - 120, dateRange, 3);

            // [FIX v5.4] เพิ่ม space ก่อน :
            DrawThaiString(gfx,"รวมระยะเวลาการอบรม :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            int workingDays = CalculateWorkingDays(data.StartDate, data.EndDate);
            DrawUnderlineText(gfx, rightColX + 130, currentY + textOffsetY, 25, workingDays.ToString(), 3);
            DrawThaiString(gfx,"วัน /", _fontSmall, XBrushes.Black, new XPoint(rightColX + 158, currentY + textOffsetY));
            DrawUnderlineText(gfx, rightColX + 185, currentY + textOffsetY, 25, data.PerPersonTrainingHours.ToString(), 3);
            DrawThaiString(gfx,"ชั่วโมง", _fontSmall, XBrushes.Black, new XPoint(rightColX + 213, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 7: สถานที่ ===
            DrawThaiString(gfx,"สถานที่ :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 50, currentY + textOffsetY, contentWidth - 60, data.TrainingLocation ?? "", 3);
            currentY += rowHeight;

            // === Row 9: โดยวิทยากร/สถาบัน ===
            // [FIX v5.4] เพิ่ม space ก่อน :
            DrawThaiString(gfx,"โดยวิทยากร/สถาบัน :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 115, currentY + textOffsetY, contentWidth - 125, data.Instructor ?? "", 3);
            currentY += rowHeight;

            // === Row 9: รายชื่อผู้เข้าอบรม ===
            currentY = DrawParticipantList(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY);

            // === Row 14: วัตถุประสงค์ ===
            currentY = DrawObjectivesSection(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY);

            // === Row 14.5: Knowledge Management (ทั้ง In-House และ Public) ===
            {
                currentY += 3;
                gfx.DrawLine(new XPen(XColors.Black, 0.5), contentX, currentY, contentX + contentWidth, currentY);
                currentY += 2;

                // หัวข้อ KM
                DrawThaiString(gfx,"แนวทางการขยายผล/การจัดการความรู้ (Knowledge Management) ภายหลังเสร็จสิ้นการอบรม กรณีอบรมภายนอก",
                    _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
                currentY += rowHeight;

                // Checkbox 1: นำส่งเอกสาร
                DrawCheckbox(gfx, labelX, currentY + 4, data.KM_SubmitDocument == true);
                DrawThaiString(gfx,"นำส่งเอกสาร และสื่อประกอบการอบรม (ถ้ามี) ให้กับแผนกฝึกอบรม เพื่อนำไปเผยแพร่ในห้องสมุดออนไลน์   (ภายใน 15 วัน หลังเสร็จสิ้นการอบรม)",
                    _fontTiny, XBrushes.Black, new XPoint(labelX + 14, currentY + textOffsetY));
                currentY += rowHeight;

                // Checkbox 2: จัดทำรายงาน/PPT + วันที่
                DrawCheckbox(gfx, labelX, currentY + 4, data.KM_CreateReport == true);
                DrawThaiString(gfx,"จัดทำเป็นรายงานหรือ PPT ส่งผู้จัดการส่วน/ฝ่าย เพื่อพิจารณา หลังจากนั้นนำส่งให้แผนกฝึกอบรม   โปรดระบุวันที่ดำเนินการ",
                    _fontTiny, XBrushes.Black, new XPoint(labelX + 14, currentY + textOffsetY));
                // [FIX v4.6] ขยายเส้นให้ยาวถึงขอบขวา
                string reportDate = data.KM_CreateReportDate?.ToString("dd/MM/yyyy") ?? "";
                double lineStartX = labelX + 420;
                double lineWidth = contentWidth - 430;
                DrawUnderlineText(gfx, lineStartX, currentY + textOffsetY, lineWidth, reportDate, 0);
                currentY += rowHeight;

                // Checkbox 3: ถ่ายทอดความรู้ + วันที่
                DrawCheckbox(gfx, labelX, currentY + 4, data.KM_KnowledgeSharing == true);
                DrawThaiString(gfx,"ถ่ายทอดความรู้ที่ได้รับจากการอบรม (Knowledge Sharing) โดยจัดบรรยายถ่ายทอดความรู้ภายในหน่วยงาน  โปรดระบุวันที่ดำเนินการ",
                    _fontTiny, XBrushes.Black, new XPoint(labelX + 14, currentY + textOffsetY));
                // [FIX v4.6] ขยายเส้นให้ยาวถึงขอบขวา
                string sharingDate = data.KM_KnowledgeSharingDate?.ToString("dd/MM/yyyy") ?? "";
                DrawUnderlineText(gfx, lineStartX, currentY + textOffsetY, lineWidth, sharingDate, 0);
                currentY += rowHeight;
            }

            // === Row 15: ผลที่คาดว่าจะได้รับ ===
            DrawThaiString(gfx,"ผลที่คาดว่าจะได้รับ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 110, currentY + textOffsetY, contentWidth - 120, data.ExpectedOutcome ?? "", 3);
            currentY += rowHeight;

            // === Row 16: งบประมาณ ===
            currentY = DrawBudgetSection(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY);

            // === เส้นแบ่งหนา ก่อนส่วนลายเซ็น ===
            currentY += 3;
            gfx.DrawLine(_thickPen, contentX, currentY, contentX + contentWidth, currentY);
            currentY += 5;

            // === Row 17: จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ + ลายเซ็น ===
            currentY = DrawSignatureSection(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY, y + sectionHeight);

            return sectionHeight;
        }

        // ==========================================
        // ส่วนที่ 2: ฝ่ายบุคคลกรอกรายละเอียดการบันทึกอบรม
        // ==========================================
        private double DrawSection2(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;
            double rowHeight = 16;  // [FIX v4.1] ลดจาก 18 → 16
            double padding = 5;
            double textOffsetY = 11; // [FIX v4.1] ลดจาก 12 → 11
            double sectionHeight = 170; // [FIX v5.2] ลดจาก 182 → 170 เพื่อเพิ่มพื้นที่ให้ Section 3 & 4

            DrawVerticalLabel(gfx, x, y, vlabelWidth, sectionHeight, "ส่วนที่ 2 ฝ่ายทรัพยากรบุคคลตรวจสอบ");
            gfx.DrawRectangle(_thickPen, contentX, y, contentWidth, sectionHeight);

            currentY = y + padding + 2;
            double labelX = contentX + padding;

            // [FIX v5.3] กำหนดตำแหน่ง checkbox ให้ตรงกันทุกแถว
            double checkboxCol1 = contentX + 130;  // ตำแหน่ง checkbox แรก
            double checkboxCol2 = contentX + 295;  // ตำแหน่ง checkbox ที่สอง

            // === Row 1: การวางแผนงบประมาณ ===
            DrawThaiString(gfx,"การวางแผนงบประมาณ :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, data.HRD_BudgetPlan == "Plan");
            DrawThaiString(gfx,"plan", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 4, data.HRD_BudgetPlan == "Unplan");
            DrawThaiString(gfx,"Unplan", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 2: การใช้งบประมาณ === [FIX v5.3] เปลี่ยนคำเป็น TYP
            DrawThaiString(gfx,"การใช้งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, data.HRD_BudgetUsage == "TYP");
            DrawThaiString(gfx,"ใช้งบประมาณตามแผน TYP", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 4, data.HRD_BudgetUsage == "Department");
            DrawThaiString(gfx,"ใช้งบต้นสังกัด (คงเหลือ)", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            string deptBudget = data.HRD_DepartmentBudgetRemaining?.ToString("N2") ?? "";
            DrawUnderlineText(gfx, checkboxCol2 + 125, currentY + textOffsetY, 50, deptBudget, 0);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 180, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 3: การเป็นสมาชิก/สิทธิพิเศษ ===
            DrawThaiString(gfx,"การเป็นสมาชิก/สิทธิพิเศษ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, data.HRD_MembershipType == "Member");
            DrawThaiString(gfx,"เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            string memberCost = data.HRD_MembershipType == "Member" ? (data.HRD_MembershipCost?.ToString("N2") ?? "") : "";
            DrawUnderlineText(gfx, checkboxCol1 + 72, currentY + textOffsetY, 40, memberCost, 0);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 117, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 4, data.HRD_MembershipType == "NonMember");
            DrawThaiString(gfx,"ไม่เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            string nonMemberCost = data.HRD_MembershipType == "NonMember" ? (data.HRD_MembershipCost?.ToString("N2") ?? "") : "";
            DrawUnderlineText(gfx, checkboxCol2 + 82, currentY + textOffsetY, 40, nonMemberCost, 0);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 127, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 4: ประวัติการอบรม (คำอธิบาย) ===
            DrawThaiString(gfx,"ประวัติการอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawThaiString(gfx,"จากการตรวจสอบประวัติการฝึกอบรมของพนักงานพบว่า (กรณีมีจำนวนมากให้แนบเอกสารเพิ่มได้)", _fontTiny, XBrushes.Black, new XPoint(labelX + 95, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 5: ตารางประวัติการอบรม ===
            currentY = DrawTrainingHistoryTable(gfx, data, contentX + padding, currentY, contentWidth - (padding * 2));

            // === Row 6-7: ตรวจสอบโดย & อนุมัติผลการตรวจสอบ ===
            currentY = DrawSection2Signatures(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY, y + sectionHeight);

            return sectionHeight;
        }

        // ==========================================
        // [FIX v3.4] ส่วนที่ 2: ลายเซ็น - จัด alignment ให้เท่ากันทั้ง 2 คอลัมน์
        // ==========================================
        private double DrawSection2Signatures(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY, double sectionBottom)
        {
            double halfWidth = contentWidth / 2;
            double labelX = contentX + padding;
            double rightColX = contentX + halfWidth;  // เริ่มตรงกลางพอดี
            double lineHeight = 16;  // [FIX v4.1] ลดจาก 18 → 16

            // กำหนดระยะห่างมาตรฐาน
            double labelToDataGap = 5;  // ระยะห่างจาก label ถึงข้อมูล

            bool isHRDAdminApproved = data.Status_HRDAdmin?.ToUpper() == "APPROVED";
            bool isHRDConfirmationApproved = data.Status_HRDConfirmation?.ToUpper() == "APPROVED";

            // === Row 1: ตรวจสอบโดย / อนุมัติผลการตรวจสอบ ===
            // คอลัมน์ซ้าย
            string leftLabel1 = "ตรวจสอบโดย:";
            DrawThaiString(gfx,leftLabel1, _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            XSize leftLabelSize1 = gfx.MeasureString(leftLabel1, _fontSmall);
            double leftDataX1 = labelX + leftLabelSize1.Width + labelToDataGap;
            double leftUnderlineWidth1 = halfWidth - leftLabelSize1.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, leftDataX1, currentY + textOffsetY + 3, leftUnderlineWidth1);

            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDAdmin))
            {
                DrawThaiString(gfx,FormatApproveInfo(data.ApproveInfo_HRDAdmin), _fontTiny, XBrushes.Black, new XPoint(leftDataX1, currentY + textOffsetY + 1));
            }

            // คอลัมน์ขวา
            string rightLabel1 = "อนุมัติผลการตรวจสอบ:";
            DrawThaiString(gfx,rightLabel1, _fontSmall, XBrushes.Black, new XPoint(rightColX + padding, currentY + textOffsetY));
            XSize rightLabelSize1 = gfx.MeasureString(rightLabel1, _fontSmall);
            double rightDataX1 = rightColX + padding + rightLabelSize1.Width + labelToDataGap;
            double rightUnderlineWidth1 = halfWidth - rightLabelSize1.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, rightDataX1, currentY + textOffsetY + 3, rightUnderlineWidth1);

            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDConfirmation))
            {
                DrawThaiString(gfx,FormatApproveInfo(data.ApproveInfo_HRDConfirmation), _fontTiny, XBrushes.Black, new XPoint(rightDataX1, currentY + textOffsetY + 1));
            }
            currentY += lineHeight;

            // === Row 2: ตำแหน่ง (ใช้ระยะห่างเดียวกัน) ===
            // คอลัมน์ซ้าย
            string leftLabel2 = "ตำแหน่ง :";
            DrawThaiString(gfx,leftLabel2, _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            XSize leftLabelSize2 = gfx.MeasureString(leftLabel2, _fontSmall);
            double leftDataX2 = labelX + leftLabelSize2.Width + labelToDataGap;
            double leftUnderlineWidth2 = halfWidth - leftLabelSize2.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, leftDataX2, currentY + textOffsetY + 3, leftUnderlineWidth2);

            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.HRDAdminLevel))
            {
                DrawThaiString(gfx,data.HRDAdminLevel, _fontSmall, XBrushes.Black, new XPoint(leftDataX2, currentY + textOffsetY));
            }

            // คอลัมน์ขวา
            string rightLabel2 = "ตำแหน่ง :";
            DrawThaiString(gfx,rightLabel2, _fontSmall, XBrushes.Black, new XPoint(rightColX + padding, currentY + textOffsetY));
            XSize rightLabelSize2 = gfx.MeasureString(rightLabel2, _fontSmall);
            double rightDataX2 = rightColX + padding + rightLabelSize2.Width + labelToDataGap;
            double rightUnderlineWidth2 = halfWidth - rightLabelSize2.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, rightDataX2, currentY + textOffsetY + 3, rightUnderlineWidth2);

            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.HRDConfirmationLevel))
            {
                DrawThaiString(gfx,data.HRDConfirmationLevel, _fontSmall, XBrushes.Black, new XPoint(rightDataX2, currentY + textOffsetY));
            }

            return currentY + lineHeight + 3;
        }

        // ==========================================
        // [FIX v5.1] ส่วนที่ 3 (ซ้าย) & ส่วนที่ 4 (ขวา) - อยู่เคียงข้างกัน
        // ==========================================
        private double DrawSection3And4(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double padding = 5;
            double textOffsetY = 10;
            double lineHeight = 13;
            double labelToDataGap = 3;
            double sectionHeight = 142; // [FIX v5.2] เพิ่มจาก 130 → 142 เพื่อให้มีพื้นที่มากขึ้น

            // คำนวณความกว้างของแต่ละส่วน
            double totalContentWidth = width - (vlabelWidth * 2); // หักพื้นที่ vertical label ทั้ง 2 ส่วน
            double halfWidth = totalContentWidth / 2;

            // ===== ส่วนที่ 3: การพิจารณาอนุมัติ (ซ้าย) =====
            double section3X = x;
            double content3X = section3X + vlabelWidth;

            DrawVerticalLabel(gfx, section3X, y, vlabelWidth, sectionHeight, "ส่วนที่ 3 การพิจารณาอนุมัติ");
            gfx.DrawRectangle(_thickPen, content3X, y, halfWidth, sectionHeight);

            double leftX = content3X + padding;
            double leftY = y + padding + 2;

            DrawThaiString(gfx,"ผลการพิจารณา :", _fontBold, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            leftY += lineHeight + 2;

            bool isDeputyManagingApproved = data.Status_DeputyManagingDirector?.ToUpper() == "APPROVED";
            bool isDeputyManagingRejected = data.Status_DeputyManagingDirector?.ToUpper() == "REJECTED";

            // Checkbox: อนุมัติ
            DrawCheckbox(gfx, leftX + 5, leftY + 2, isDeputyManagingApproved);
            DrawThaiString(gfx,"อนุมัติให้ฝึกอบรมสัมมนา", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight;

            // Checkbox: ไม่อนุมัติ
            DrawCheckbox(gfx, leftX + 5, leftY + 2, isDeputyManagingRejected);
            DrawThaiString(gfx,"ไม่อนุมัติ/ส่งกลับให้ต้นสังกัดทบทวนใหม่", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight + 2;

            // เหตุผล - Multi-line (3 บรรทัด)
            string reasonLabel = "เหตุผล :";
            DrawThaiString(gfx,reasonLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize reasonLabelSize = gfx.MeasureString(reasonLabel, _fontSmall);
            double reasonDataX = leftX + reasonLabelSize.Width + labelToDataGap;
            double reasonMaxWidth = halfWidth - reasonLabelSize.Width - labelToDataGap - padding * 2;

            // แสดง Comment_DeputyManagingDirector พร้อม text wrapping
            string reasonText = data.Comment_DeputyManagingDirector ?? "";
            if (!string.IsNullOrEmpty(reasonText))
            {
                DrawMultilineText(gfx, reasonDataX, leftY + textOffsetY, reasonMaxWidth, reasonText, _fontSmall, 3);
            }
            // วาดเส้นใต้ 3 บรรทัด
            DrawUnderline(gfx, reasonDataX, leftY + textOffsetY + 3, reasonMaxWidth);
            leftY += lineHeight;
            DrawUnderline(gfx, reasonDataX, leftY + textOffsetY + 3, reasonMaxWidth);
            leftY += lineHeight;
            DrawUnderline(gfx, reasonDataX, leftY + textOffsetY + 3, reasonMaxWidth);
            leftY += lineHeight + 5;

            // ลงนาม
            string signLabel = "ลงนาม:";
            DrawThaiString(gfx,signLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize signLabelSize = gfx.MeasureString(signLabel, _fontSmall);
            double signDataX = leftX + signLabelSize.Width + labelToDataGap;
            double signUnderlineWidth = halfWidth - signLabelSize.Width - labelToDataGap - padding * 2 - 10;
            DrawUnderline(gfx, signDataX, leftY + textOffsetY + 3, signUnderlineWidth);

            if ((isDeputyManagingApproved || isDeputyManagingRejected) && !string.IsNullOrEmpty(data.DeputyManagingDirectorId))
            {
                XSize dataSize = gfx.MeasureString(data.DeputyManagingDirectorId, _fontSmall);
                double centerX = signDataX + (signUnderlineWidth / 2) - (dataSize.Width / 2);
                DrawThaiString(gfx,data.DeputyManagingDirectorId, _fontSmall, XBrushes.Black, new XPoint(centerX, leftY + textOffsetY));
            }
            leftY += lineHeight;

            // วงเล็บ (วันที่อนุมัติ)
            if ((isDeputyManagingApproved || isDeputyManagingRejected) && !string.IsNullOrEmpty(data.ApproveInfo_DeputyManagingDirector))
            {
                string approveText = $"( {FormatApproveInfo(data.ApproveInfo_DeputyManagingDirector)} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = signDataX + (signUnderlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, leftY + textOffsetY - 5));
            }
            leftY += lineHeight - 5;

            // ตำแหน่ง
            string posLabel = "ตำแหน่ง :";
            DrawThaiString(gfx,posLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize posLabelSize = gfx.MeasureString(posLabel, _fontSmall);
            double posDataX = leftX + posLabelSize.Width + labelToDataGap;
            double posUnderlineWidth = halfWidth - posLabelSize.Width - labelToDataGap - padding * 2 - 10;
            DrawUnderline(gfx, posDataX, leftY + textOffsetY + 3, posUnderlineWidth);

            if ((isDeputyManagingApproved || isDeputyManagingRejected) && !string.IsNullOrEmpty(data.DeputyManagingDirectorLevel))
            {
                DrawThaiString(gfx,data.DeputyManagingDirectorLevel, _fontSmall, XBrushes.Black, new XPoint(posDataX, leftY + textOffsetY));
            }

            // ===== ส่วนที่ 4: การดำเนินงานหลังอนุมัติ (ขวา) =====
            double section4X = content3X + halfWidth;
            double content4X = section4X + vlabelWidth;
            double content4Width = halfWidth;

            DrawVerticalLabel(gfx, section4X, y, vlabelWidth, sectionHeight, "ส่วนที่ 4 การดำเนินงานหลังอนุมัติ");
            gfx.DrawRectangle(_thickPen, content4X, y, content4Width, sectionHeight);

            double rightX = content4X + padding;
            double rightY = y + padding + 2;

            DrawThaiString(gfx,"ข้อมูลส่วน HRD บันทึกข้อมูล", _fontBold, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            rightY += lineHeight + 2;

            // [FIX v5.4] คำนวณความยาวเส้นใต้มาตรฐาน (ยาวเต็มถึงขอบ)
            double fullUnderlineWidth = content4Width - padding * 2 - 5;

            // ติดต่อสถาบัน/ผู้สอน : วันที่
            string contactLabel = "- ติดต่อสถาบัน/ผู้สอน : วันที่";
            DrawThaiString(gfx,contactLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize contactLabelSize = gfx.MeasureString(contactLabel, _fontSmall);
            double contactDataX = rightX + contactLabelSize.Width + labelToDataGap;
            DrawUnderline(gfx, contactDataX, rightY + textOffsetY + 3, fullUnderlineWidth - contactLabelSize.Width - labelToDataGap);
            if (data.HRD_ContactDate.HasValue)
            {
                DrawThaiString(gfx,data.HRD_ContactDate.Value.ToString("dd/MM/yyyy"), _fontSmall, XBrushes.Black, new XPoint(contactDataX, rightY + textOffsetY));
            }
            rightY += lineHeight;

            // ชื่อผู้ที่ติดต่อด้วย - [FIX v5.4] เส้นยาวเต็มเหมือน ติดต่อสถาบัน/ผู้สอน
            string nameLabel = "  ชื่อผู้ที่ติดต่อด้วย";
            DrawThaiString(gfx,nameLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize nameLabelSize = gfx.MeasureString(nameLabel, _fontSmall);
            double nameDataX = rightX + nameLabelSize.Width + labelToDataGap;
            DrawUnderline(gfx, nameDataX, rightY + textOffsetY + 3, fullUnderlineWidth - nameLabelSize.Width - labelToDataGap);
            if (!string.IsNullOrEmpty(data.HRD_ContactPerson))
            {
                DrawThaiString(gfx,data.HRD_ContactPerson, _fontSmall, XBrushes.Black, new XPoint(nameDataX, rightY + textOffsetY));
            }
            rightY += lineHeight;

            // [FIX v5.5] การชำระเงิน + 3 checkbox + เส้นใต้
            string payLabel = "- การชำระเงิน";
            DrawThaiString(gfx,payLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize payLabelSize = gfx.MeasureString(payLabel, _fontSmall);
            // วาดเส้นใต้จากหลัง label ไปถึงท้ายสุด
            DrawUnderline(gfx, rightX + payLabelSize.Width + 3, rightY + textOffsetY + 3, fullUnderlineWidth - payLabelSize.Width - 3);

            bool isCheck = data.HRD_PaymentMethod == "Check";
            bool isTransfer = data.HRD_PaymentMethod == "Transfer";
            bool isCash = data.HRD_PaymentMethod == "Cash";

            // 3 checkbox ในแนวนอน (เช็ค, โอนเงิน, เงินสด) - วาดบนเส้นใต้
            double cbPayX = rightX + 70;
            DrawCheckbox(gfx, cbPayX, rightY + 2, isCheck);
            DrawThaiString(gfx,"เช็ค", _fontSmall, XBrushes.Black, new XPoint(cbPayX + 14, rightY + textOffsetY));
            cbPayX += 45;
            DrawCheckbox(gfx, cbPayX, rightY + 2, isTransfer);
            DrawThaiString(gfx,"โอนเงิน", _fontSmall, XBrushes.Black, new XPoint(cbPayX + 14, rightY + textOffsetY));
            cbPayX += 55;
            double cashCheckboxX = cbPayX; // [FIX v5.5] จำตำแหน่ง เงินสด ไว้ใช้อ้างอิง
            DrawCheckbox(gfx, cbPayX, rightY + 2, isCash);
            DrawThaiString(gfx,"เงินสด", _fontSmall, XBrushes.Black, new XPoint(cbPayX + 14, rightY + textOffsetY));
            rightY += lineHeight;

            // ภายในวันที่ - [FIX v5.4] เส้นยาวเต็ม
            string payDateLabel = "  ภายในวันที่";
            DrawThaiString(gfx,payDateLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize payDateLabelSize = gfx.MeasureString(payDateLabel, _fontSmall);
            double payDateDataX = rightX + payDateLabelSize.Width + labelToDataGap;
            DrawUnderline(gfx, payDateDataX, rightY + textOffsetY + 3, fullUnderlineWidth - payDateLabelSize.Width - labelToDataGap);
            if (data.HRD_PaymentDate.HasValue)
            {
                DrawThaiString(gfx,data.HRD_PaymentDate.Value.ToString("dd/MM/yyyy"), _fontSmall, XBrushes.Black, new XPoint(payDateDataX, rightY + textOffsetY));
            }
            rightY += lineHeight;

            // [FIX v5.6] 3 checkbox ใหม่ - เส้นใต้สิ้นสุดที่ checkbox, checkbox ตรงกับ เงินสด
            // cashCheckboxX คือตำแหน่ง checkbox ของ "เงินสด" ด้านบน
            // เส้นใต้ต้องยาวจาก label ไปถึง checkbox (รวม checkbox ด้วย)
            double underlineEndX = cashCheckboxX + 15; // สิ้นสุดหลัง checkbox

            string trainingRecordLabel = "- บันทึกประวัติฝึกอบรม Training Record :";
            DrawThaiString(gfx,trainingRecordLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            double trLabelWidth = gfx.MeasureString(trainingRecordLabel, _fontSmall).Width;
            double trUnderlineStart = rightX + trLabelWidth + 3;
            DrawUnderline(gfx, trUnderlineStart, rightY + textOffsetY + 3, underlineEndX - trUnderlineStart);
            DrawCheckbox(gfx, cashCheckboxX, rightY + 2, data.HRD_TrainingRecord == true);
            rightY += lineHeight;

            string kmLabel = "- การจัดการความรู้ (KM) :";
            DrawThaiString(gfx,kmLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            double kmLabelWidth = gfx.MeasureString(kmLabel, _fontSmall).Width;
            double kmUnderlineStart = rightX + kmLabelWidth + 3;
            DrawUnderline(gfx, kmUnderlineStart, rightY + textOffsetY + 3, underlineEndX - kmUnderlineStart);
            DrawCheckbox(gfx, cashCheckboxX, rightY + 2, data.HRD_KnowledgeManagementDone == true);
            rightY += lineHeight;

            string certLabel = "- การยื่นขอรับรองหลักสูตร :";
            DrawThaiString(gfx,certLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            double certLabelWidth = gfx.MeasureString(certLabel, _fontSmall).Width;
            double certUnderlineStart = rightX + certLabelWidth + 3;
            DrawUnderline(gfx, certUnderlineStart, rightY + textOffsetY + 3, underlineEndX - certUnderlineStart);
            DrawCheckbox(gfx, cashCheckboxX, rightY + 2, data.HRD_CourseCertification == true);
            rightY += lineHeight + 2;

            // ลงชื่อ + ผู้บันทึก
            string signLabel2 = "ลงชื่อ :";
            DrawThaiString(gfx,signLabel2, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize signLabel2Size = gfx.MeasureString(signLabel2, _fontSmall);
            double signDataX2 = rightX + signLabel2Size.Width + labelToDataGap;
            double signUnderlineWidth2 = content4Width - signLabel2Size.Width - labelToDataGap - padding * 2 - 50;
            DrawUnderline(gfx, signDataX2, rightY + textOffsetY + 3, signUnderlineWidth2);
            if (!string.IsNullOrEmpty(data.HRD_RecorderSignature))
            {
                DrawThaiString(gfx,data.HRD_RecorderSignature, _fontSmall, XBrushes.Black, new XPoint(signDataX2, rightY + textOffsetY));
            }
            DrawThaiString(gfx,"ผู้บันทึก", _fontSmall, XBrushes.Black, new XPoint(signDataX2 + signUnderlineWidth2 + 5, rightY + textOffsetY));

            return sectionHeight;
        }

        // ==========================================
        // [NEW v5.0] Helper: Draw Multi-line Text with wrapping
        // ==========================================
        private void DrawMultilineText(XGraphics gfx, double x, double y, double maxWidth, string text, XFont font, int maxLines)
        {
            if (string.IsNullOrEmpty(text)) return;

            double lineHeight = 13;
            List<string> lines = WrapText(gfx, text, font, maxWidth);

            for (int i = 0; i < Math.Min(lines.Count, maxLines); i++)
            {
                DrawThaiString(gfx,lines[i], font, XBrushes.Black, new XPoint(x, y + (i * lineHeight)));
            }
        }

        // ==========================================
        // [NEW v5.0] Helper: Wrap text to fit width
        // ==========================================
        private List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;

            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                XSize size = gfx.MeasureString(testLine, font);

                if (size.Width <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                    }
                    currentLine = word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        // ==========================================
        // Thai Text Wrapper
        // Loma font รองรับภาษาไทยได้ถูกต้องโดยไม่ต้องใช้ PUA remapping
        // ==========================================
        private void DrawThaiString(XGraphics gfx, string text, XFont font, XBrush brush, XPoint point)
        {
            gfx.DrawString(text, font, brush, point);
        }

        private void DrawThaiString(XGraphics gfx, string text, XFont font, XBrush brush, XRect rect, XStringFormat format)
        {
            gfx.DrawString(text, font, brush, rect, format);
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private void DrawVerticalLabel(XGraphics gfx, double x, double y, double width, double height, string text)
        {
            gfx.DrawRectangle(_grayBrush, x, y, width, height);
            gfx.DrawRectangle(_thickPen, x, y, width, height);

            XGraphicsState state = gfx.Save();
            gfx.TranslateTransform(x + width / 2 + 3, y + height / 2);
            gfx.RotateTransform(-90);

            XSize textSize = gfx.MeasureString(text, _fontSmall);
            DrawThaiString(gfx,text, _fontSmall, XBrushes.Black, new XPoint(-textSize.Width / 2, 0));

            gfx.Restore(state);
        }

        private void DrawCheckbox(XGraphics gfx, double x, double y, bool isChecked)
        {
            double size = 9;
            gfx.DrawRectangle(_borderPen, x, y, size, size);
            if (isChecked)
            {
                gfx.DrawLine(new XPen(XColors.Black, 1.2), x + 2, y + 4.5, x + 3.5, y + 7);
                gfx.DrawLine(new XPen(XColors.Black, 1.2), x + 3.5, y + 7, x + 7, y + 2);
            }
        }

        private void DrawUnderline(XGraphics gfx, double x, double y, double width)
        {
            gfx.DrawLine(_dottedPen, x, y, x + width, y);
        }

        private void DrawUnderlineText(XGraphics gfx, double x, double y, double width, string text, double lineOffset = 2)
        {
            DrawThaiString(gfx,text ?? "", _fontSmall, XBrushes.Black, new XPoint(x, y));
            gfx.DrawLine(_dottedPen, x, y + lineOffset, x + width, y + lineOffset);
        }

        /// <summary>
        /// ตัดส่วน @domain.com ออกจาก ApproveInfo
        /// จาก: "email@gmail.com / 18/01/2026 / 22:23"
        /// เป็น: "email / 18/01/2026 / 22:23"
        /// </summary>
        private string FormatApproveInfo(string approveInfo)
        {
            if (string.IsNullOrEmpty(approveInfo))
                return approveInfo;

            // หา @ และตัดส่วน @domain.com ออกจนถึง /
            int atIndex = approveInfo.IndexOf('@');
            if (atIndex > 0)
            {
                int slashIndex = approveInfo.IndexOf('/', atIndex);
                if (slashIndex > atIndex)
                {
                    // ตัดส่วน @domain.com ออก (รวม space ก่อน /)
                    string username = approveInfo.Substring(0, atIndex);
                    string rest = approveInfo.Substring(slashIndex - 1); // เอา space และ / ต่อไป
                    return username + rest;
                }
            }
            return approveInfo;
        }

        private double DrawParticipantList(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 15;  // [FIX v4.1] ลดจาก 17 → 15
            double labelX = contentX + padding;

            DrawThaiString(gfx,"รายชื่อ", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            currentY += rowHeight;

            for (int i = 0; i < 3; i++)
            {
                double xPos = labelX + 25;
                DrawThaiString(gfx,$"{i + 1}.", _fontSmall, XBrushes.Black, new XPoint(labelX + 10, currentY + textOffsetY));

                if (i < data.Employees.Count)
                {
                    var emp = data.Employees[i];
                    DrawUnderlineText(gfx, xPos, currentY + textOffsetY, 145, emp.EmployeeName ?? "", 3);
                    xPos += 155;
                    DrawThaiString(gfx,"รหัส :", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderlineText(gfx, xPos + 35, currentY + textOffsetY, 60, emp.EmployeeCode ?? "", 3);
                    xPos += 105;
                    DrawThaiString(gfx,"ตำแหน่ง:", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderlineText(gfx, xPos + 50, currentY + textOffsetY, contentWidth - xPos - 55 + contentX, emp.Level ?? "", 3);
                }
                else
                {
                    DrawUnderline(gfx, xPos, currentY + textOffsetY + 3, 145);
                    xPos += 155;
                    DrawThaiString(gfx,"รหัส :", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderline(gfx, xPos + 35, currentY + textOffsetY + 3, 60);
                    xPos += 105;
                    DrawThaiString(gfx,"ตำแหน่ง:", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderline(gfx, xPos + 50, currentY + textOffsetY + 3, contentWidth - xPos - 55 + contentX);
                }

                currentY += rowHeight;
            }

            return currentY;
        }

        private double DrawObjectivesSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 15;  // [FIX v4.1] ลดจาก 17 → 15
            double labelX = contentX + padding;

            string objective = data.TrainingObjective ?? "";
            bool isObj1 = objective.Contains("พัฒนาทักษะ");
            bool isObj2 = objective.Contains("เพิ่มประสิทธิภาพ") || objective.Contains("คุณภาพ");
            bool isObj3 = objective.Contains("แก้ไข") || objective.Contains("ป้องกันปัญหา");
            bool isObj4 = objective.Contains("กฎหมาย") || objective.Contains("ข้อกำหนด");
            bool isObj5 = objective.Contains("ถ่ายทอดความรู้") || objective.Contains("ขยายผล");
            bool isObj6 = objective.Contains("อื่นๆ");

            DrawThaiString(gfx,"วัตถุประสงค์:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            // === Row 1: พัฒนาทักษะ + เพิ่มประสิทธิภาพ + ช่วยแก้ไข + อื่นๆ (ระบุ) ===
            double cbX = labelX + 75;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj1);
            DrawThaiString(gfx,"พัฒนาทักษะความชำนาญ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 125;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj2);
            DrawThaiString(gfx,"เพิ่มประสิทธิภาพ / คุณภาพ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 135;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj3);
            DrawThaiString(gfx,"ช่วยแก้ไข / ป้องกันปัญหา", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // ย้าย "อื่นๆ (ระบุ)" มาอยู่บรรทัดเดียวกับ "ช่วยแก้ไข / ป้องกันปัญหา"
            currentY += rowHeight;
            cbX = labelX + 75;

            // === Row 2: กฎหมาย + ถ่ายทอดความรู้ + อื่นๆ (ระบุ) ===
            DrawCheckbox(gfx, cbX, currentY + 4, isObj4);
            DrawThaiString(gfx,"กฎหมาย/ข้อกำหนดลูกค้า", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 125;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj5);
            DrawThaiString(gfx,"ถ่ายทอดความรู้/ขยายผลสู่ผู้อื่น", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // [FIX v4.4] ปรับ อื่นๆ (ระบุ) ให้ตรงกับ ช่วยแก้ไข / ป้องกันปัญหา (เปลี่ยน +175 → +135)
            cbX += 135;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj6);
            DrawThaiString(gfx,"อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 72, currentY + textOffsetY + 3, contentWidth - cbX - 77 + contentX);

            currentY += rowHeight;
            return currentY;
        }

        // ==========================================
        // [FIX v2.6] DrawBudgetSection - ปรับ alignment ให้ตรงกัน
        // ==========================================
        private double DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 15;  // [FIX v4.1] ลดจาก 17 → 15
            double labelX = contentX + padding;

            // [FIX v2.6] กำหนดตำแหน่งคอลัมน์ที่ชัดเจนและสม่ำเสมอ
            double col1X = labelX + 68;           // คอลัมน์ซ้าย (checkbox)
            double col1TextX = col1X + 14;        // ข้อความหลัง checkbox
            double col1ValueX = col1X + 95;       // ตำแหน่งค่าตัวเลข (คอลัมน์ซ้าย)
            double col1BahtX = col1X + 150;       // ตำแหน่ง "บาท" (คอลัมน์ซ้าย)

            double col2X = labelX + 243;          // คอลัมน์ขวา (checkbox)
            double col2TextX = col2X + 14;        // ข้อความหลัง checkbox
            double col2ValueX = col2X + 80;       // ตำแหน่งค่าตัวเลข (คอลัมน์ขวา)
            double col2BahtX = col2X + 135;       // ตำแหน่ง "บาท" (คอลัมน์ขวา)

            double underlineWidth = 50;           // ความกว้างเส้นใต้มาตรฐาน

            DrawThaiString(gfx,"งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            // === แถว 1: ค่าลงทะเบียน/วิทยากร + ค่าวิทยากร ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.RegistrationCost > 0);
            DrawThaiString(gfx,"ค่าลงทะเบียน/วิทยากร", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.RegistrationCost.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.InstructorFee > 0);
            DrawThaiString(gfx,"ค่าวิทยากร", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, underlineWidth, data.InstructorFee.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col2BahtX, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 2: ค่าอาหาร + ค่าอุปกรณ์ ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.FoodCost > 0);
            DrawThaiString(gfx,"ค่าอาหาร", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.FoodCost.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.EquipmentCost > 0);
            DrawThaiString(gfx,"ค่าอุปกรณ์", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, underlineWidth, data.EquipmentCost.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col2BahtX, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 3: ค่าใช้จ่ายต่อคน + อื่นๆ (ระบุ) ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.CostPerPerson > 0);
            DrawThaiString(gfx,"ค่าใช้จ่ายต่อคน", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.CostPerPerson.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.OtherCost > 0);
            DrawThaiString(gfx,"อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));

            // แสดง OtherCostDescription + OtherCost
            string otherText = "";
            if (!string.IsNullOrEmpty(data.OtherCostDescription))
                otherText = $"{data.OtherCostDescription} {data.OtherCost:N0}";
            else if (data.OtherCost > 0)
                otherText = data.OtherCost.ToString("N0");

            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, 100, otherText, 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col2ValueX + 105, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 4: รวมสุทธิ (ตรงกับ ค่าใช้จ่ายต่อคน) + ลงชื่อผู้ขออบรม ===
            // [FIX v4.3] ขยับ รวมสุทธิ มาตรงกับ ค่าใช้จ่ายต่อคน (col1)
            DrawThaiString(gfx,"รวมสุทธิ:", _fontBold, XBrushes.Black, new XPoint(col1X, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.TotalCost.ToString("N0"), 3);
            DrawThaiString(gfx,"บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            // [FIX v5.7] เพิ่ม ลงชื่อผู้ขออบรม + แสดง CreatedBy - เส้นใต้ยาวถึงขอบ
            DrawThaiString(gfx,"ลงชื่อผู้ขออบรม:", _fontBold, XBrushes.Black, new XPoint(col2X, currentY + textOffsetY));
            double signUnderlineWidth = contentWidth - (col2ValueX - contentX) - padding;
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, signUnderlineWidth, data.CreatedBy ?? "", 3);
            currentY += rowHeight;

            return currentY;
        }

        /// <summary>
        /// คำนวณจำนวนวันทำงาน (ไม่นับวันเสาร์-อาทิตย์)
        /// </summary>
        private int CalculateWorkingDays(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return 0;

            if (startDate.Value > endDate.Value)
                return 0;

            int days = 0;
            for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
            {
                // ไม่นับวันเสาร์ (Saturday) และวันอาทิตย์ (Sunday)
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    days++;
            }

            return days;
        }

        // ==========================================
        // [FIX v3.6] DrawSignatureSection - 3 คอลัมน์ (Section Manager, Department Manager, Managing Director)
        // ==========================================
        private double DrawSignatureSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY, double sectionBottom)
        {
            // แบ่งเป็น 3 คอลัมน์
            double colWidth = contentWidth / 3;
            double col1X = contentX + padding;           // Section Manager
            double col2X = contentX + colWidth + padding; // Department Manager
            double col3X = contentX + (colWidth * 2) + padding; // Managing Director
            double lineHeight = 13;  // [FIX v4.1] ลดจาก 15 → 13

            // === ROW 1: หัวข้อทั้ง 3 คอลัมน์ ===
            // [FIX v4.4] เปลี่ยนหัวคอลัมน์ตามโครงสร้างองค์กร
            DrawThaiString(gfx,"ต้นสังกัดพิจารณา (ผู้จัดการส่วน)", _fontBold, XBrushes.Black, new XPoint(col1X, currentY + textOffsetY));
            DrawThaiString(gfx,"ต้นสังกัดพิจารณา (ผู้จัดการฝ่าย)", _fontBold, XBrushes.Black, new XPoint(col2X, currentY + textOffsetY));
            DrawThaiString(gfx,"ต้นสังกัดทบทวน (ผู้อำนวยการฝ่าย)", _fontBold, XBrushes.Black, new XPoint(col3X, currentY + textOffsetY));
            currentY += lineHeight + 2;

            // === ROW 2: Checkbox อนุมัติ/ไม่อนุมัติ ===
            bool isSectionApproved = data.Status_SectionManager?.ToUpper() == "APPROVED";
            bool isSectionRejected = data.Status_SectionManager?.ToUpper() == "REJECTED";
            bool isDepartmentApproved = data.Status_DepartmentManager?.ToUpper() == "APPROVED";
            bool isDepartmentRejected = data.Status_DepartmentManager?.ToUpper() == "REJECTED";
            bool isManagingApproved = data.Status_ManagingDirector?.ToUpper() == "APPROVED";
            bool isManagingRejected = data.Status_ManagingDirector?.ToUpper() == "REJECTED";

            // Column 1: Section Manager
            double cbX = col1X;
            DrawCheckbox(gfx, cbX, currentY + 4, isSectionApproved);
            DrawThaiString(gfx,"อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 50;
            DrawCheckbox(gfx, cbX, currentY + 4, isSectionRejected);
            DrawThaiString(gfx,"ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // Column 2: Department Manager
            cbX = col2X;
            DrawCheckbox(gfx, cbX, currentY + 4, isDepartmentApproved);
            DrawThaiString(gfx,"อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 50;
            DrawCheckbox(gfx, cbX, currentY + 4, isDepartmentRejected);
            DrawThaiString(gfx,"ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // Column 3: Managing Director
            cbX = col3X;
            DrawCheckbox(gfx, cbX, currentY + 4, isManagingApproved);
            DrawThaiString(gfx,"อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 50;
            DrawCheckbox(gfx, cbX, currentY + 4, isManagingRejected);
            DrawThaiString(gfx,"ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            currentY += lineHeight + 1;

            // === ROW 3: ลงชื่อ ===
            double underlineWidth = colWidth - 50;

            // Column 1: Section Manager
            DrawThaiString(gfx,"ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(col1X, currentY + textOffsetY));
            DrawUnderline(gfx, col1X + 35, currentY + textOffsetY + 3, underlineWidth);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerId))
            {
                XSize textSize = gfx.MeasureString(data.SectionManagerId, _fontTiny);
                double centerX = col1X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,data.SectionManagerId, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY));
            }

            // Column 2: Department Manager
            DrawThaiString(gfx,"ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(col2X, currentY + textOffsetY));
            DrawUnderline(gfx, col2X + 35, currentY + textOffsetY + 3, underlineWidth);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerId))
            {
                XSize textSize = gfx.MeasureString(data.DepartmentManagerId, _fontTiny);
                double centerX = col2X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,data.DepartmentManagerId, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY));
            }

            // Column 3: Managing Director
            DrawThaiString(gfx,"ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(col3X, currentY + textOffsetY));
            DrawUnderline(gfx, col3X + 35, currentY + textOffsetY + 3, underlineWidth);
            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorId))
            {
                XSize textSize = gfx.MeasureString(data.ManagingDirectorId, _fontTiny);
                double centerX = col3X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,data.ManagingDirectorId, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY));
            }
            currentY += lineHeight;

            // === ROW 4: ApproveInfo (วงเล็บ) ===
            // [FIX v4.2] เพิ่มระยะห่างจากเส้นลงชื่อ (ลบ -5 → -2)
            // Column 1
            if (isSectionApproved && !string.IsNullOrEmpty(data.ApproveInfo_SectionManager))
            {
                string approveText = $"( {FormatApproveInfo(data.ApproveInfo_SectionManager)} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = col1X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 2));
            }

            // Column 2
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.ApproveInfo_DepartmentManager))
            {
                string approveText = $"( {FormatApproveInfo(data.ApproveInfo_DepartmentManager)} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = col2X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 2));
            }

            // Column 3
            if (isManagingApproved && !string.IsNullOrEmpty(data.ApproveInfo_ManagingDirector))
            {
                string approveText = $"( {FormatApproveInfo(data.ApproveInfo_ManagingDirector)} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = col3X + 35 + (underlineWidth / 2) - (textSize.Width / 2);
                DrawThaiString(gfx,approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 2));
            }
            currentY += lineHeight - 3;

            // === ROW 5: ตำแหน่ง ===
            double posUnderlineWidth = colWidth - 60;

            // Column 1
            DrawThaiString(gfx,"ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(col1X, currentY + textOffsetY));
            DrawUnderline(gfx, col1X + 45, currentY + textOffsetY + 3, posUnderlineWidth);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerLevel))
            {
                DrawThaiString(gfx,data.SectionManagerLevel, _fontTiny, XBrushes.Black, new XPoint(col1X + 45, currentY + textOffsetY));
            }

            // Column 2
            DrawThaiString(gfx,"ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(col2X, currentY + textOffsetY));
            DrawUnderline(gfx, col2X + 45, currentY + textOffsetY + 3, posUnderlineWidth);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerLevel))
            {
                DrawThaiString(gfx,data.DepartmentManagerLevel, _fontTiny, XBrushes.Black, new XPoint(col2X + 45, currentY + textOffsetY));
            }

            // Column 3
            DrawThaiString(gfx,"ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(col3X, currentY + textOffsetY));
            DrawUnderline(gfx, col3X + 45, currentY + textOffsetY + 3, posUnderlineWidth);
            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorLevel))
            {
                DrawThaiString(gfx,data.ManagingDirectorLevel, _fontTiny, XBrushes.Black, new XPoint(col3X + 45, currentY + textOffsetY));
            }

            // ไม่มีเส้นแบ่งกลาง ตามที่ผู้ใช้ต้องการ

            return currentY + lineHeight;
        }

        private double DrawTrainingHistoryTable(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            string[] headers = { "ที่", "รหัสพนักงาน", "ชื่อ - สกุล", "ไม่เคย", "เคย", "ใกล้เคียง", "เมื่อวันที่", "ชื่อหลักสูตร" };
            double[] colWidths = { 20, 65, 115, 35, 30, 45, 55, width - 365 };
            double rowHeight = 14;  // [FIX v4.1] ลดจาก 15 → 14

            // Header row
            double xCol = x;
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, y, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, y, colWidths[i], rowHeight);
                DrawThaiString(gfx,headers[i], _fontTiny, XBrushes.Black,
                    new XRect(xCol, y + 4, colWidths[i], rowHeight), XStringFormats.TopCenter);
                xCol += colWidths[i];
            }

            y += rowHeight;

            // Data rows - แสดงข้อมูลจริงจาก TrainingHistories (ถ้ามี) หรือ empty rows (ขั้นต่ำ 3 แถว)
            int totalRows = Math.Max(3, data.TrainingHistories?.Count ?? 0);
            for (int row = 0; row < totalRows; row++)
            {
                xCol = x;
                var historyItem = (data.TrainingHistories != null && row < data.TrainingHistories.Count) ? data.TrainingHistories[row] : null;

                for (int col = 0; col < colWidths.Length; col++)
                {
                    gfx.DrawRectangle(_borderPen, xCol, y, colWidths[col], rowHeight);

                    if (col == 0)
                    {
                        DrawThaiString(gfx,(row + 1).ToString(), _fontTiny, XBrushes.Black,
                            new XRect(xCol, y + 4, colWidths[col], rowHeight), XStringFormats.TopCenter);
                    }
                    else if (col == 1 && historyItem != null)
                    {
                        DrawThaiString(gfx,historyItem.EmployeeCode ?? "", _fontTiny, XBrushes.Black,
                            new XRect(xCol + 2, y + 4, colWidths[col] - 4, rowHeight), XStringFormats.TopLeft);
                    }
                    else if (col == 2 && historyItem != null)
                    {
                        DrawThaiString(gfx,historyItem.EmployeeName ?? "", _fontTiny, XBrushes.Black,
                            new XRect(xCol + 2, y + 4, colWidths[col] - 4, rowHeight), XStringFormats.TopLeft);
                    }
                    else if (col == 3) // ไม่เคย
                    {
                        double cbXPos = xCol + (colWidths[col] / 2) - 4;
                        double cbY = y + 3;
                        DrawCheckbox(gfx, cbXPos, cbY, historyItem?.HistoryType == "Never");
                    }
                    else if (col == 4) // เคย
                    {
                        double cbXPos = xCol + (colWidths[col] / 2) - 4;
                        double cbY = y + 3;
                        DrawCheckbox(gfx, cbXPos, cbY, historyItem?.HistoryType == "Ever");
                    }
                    else if (col == 5) // ใกล้เคียง
                    {
                        double cbXPos = xCol + (colWidths[col] / 2) - 4;
                        double cbY = y + 3;
                        DrawCheckbox(gfx, cbXPos, cbY, historyItem?.HistoryType == "Similar");
                    }
                    else if (col == 6 && historyItem?.TrainingDate != null)
                    {
                        DrawThaiString(gfx,historyItem.TrainingDate?.ToString("dd/MM/yyyy") ?? "", _fontTiny, XBrushes.Black,
                            new XRect(xCol + 2, y + 4, colWidths[col] - 4, rowHeight), XStringFormats.TopCenter);
                    }
                    else if (col == 7 && historyItem != null)
                    {
                        DrawThaiString(gfx,historyItem.CourseName ?? "", _fontTiny, XBrushes.Black,
                            new XRect(xCol + 2, y + 4, colWidths[col] - 4, rowHeight), XStringFormats.TopLeft);
                    }

                    xCol += colWidths[col];
                }
                y += rowHeight;
            }

            return y + 8; // เพิ่มระยะห่างก่อนลายเซ็น
        }

        // ==========================================
        // DATABASE METHODS
        // ==========================================

        private async Task<TrainingRequestData> GetTrainingRequestDataAsync(int id)
        {
            var data = new TrainingRequestData();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT
                        tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.Department, tr.Position,
                        tr.SeminarTitle, tr.TrainingLocation, tr.Instructor,
                        tr.StartDate, tr.EndDate, tr.TotalPeople, tr.PerPersonTrainingHours,
                        tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost, tr.FoodCost, tr.OtherCost,
                        tr.OtherCostDescription,
                        tr.TotalCost, tr.CostPerPerson,
                        tr.TrainingObjective, tr.OtherObjective, tr.ExpectedOutcome,
                        tr.TravelMethod, tr.TargetGroup,
                        tr.Status, tr.CreatedBy, tr.CreatedDate,
                        tr.SectionManagerId, tr.Status_SectionManager, tr.ApproveInfo_SectionManager,
                        tr.DepartmentManagerId, tr.Status_DepartmentManager, tr.ApproveInfo_DepartmentManager,
                        tr.HRDAdminid AS HRDAdminId, tr.Status_HRDAdmin, tr.ApproveInfo_HRDAdmin,
                        tr.HRDConfirmationid AS HRDConfirmationId, tr.Status_HRDConfirmation, tr.ApproveInfo_HRDConfirmation,
                        tr.ManagingDirectorId, tr.Status_ManagingDirector, tr.ApproveInfo_ManagingDirector,
                        tr.DeputyManagingDirectorId, tr.Status_DeputyManagingDirector, tr.ApproveInfo_DeputyManagingDirector,
                        tr.KM_SubmitDocument, tr.KM_CreateReport, tr.KM_CreateReportDate, tr.KM_KnowledgeSharing, tr.KM_KnowledgeSharingDate,
                        tr.HRD_ContactDate, tr.HRD_ContactPerson, tr.HRD_PaymentDate, tr.HRD_PaymentMethod, tr.HRD_RecorderSignature,
                        tr.HRD_BudgetPlan, tr.HRD_BudgetUsage, tr.HRD_DepartmentBudgetRemaining, tr.HRD_MembershipType, tr.HRD_MembershipCost,
                        tr.HRD_TrainingRecord, tr.HRD_KnowledgeManagementDone, tr.HRD_CourseCertification,
                        tr.Comment_DeputyManagingDirector,
                        e_sm.Level AS SectionManagerLevel,
                        e_dm.Level AS DepartmentManagerLevel,
                        e_ha.Level AS HRDAdminLevel,
                        e_hc.Level AS HRDConfirmationLevel,
                        e_md.Level AS ManagingDirectorLevel,
                        e_dmd.Level AS DeputyManagingDirectorLevel
                    FROM TrainingRequests tr
                    LEFT JOIN Employees e_sm ON tr.SectionManagerId = e_sm.Email
                    LEFT JOIN Employees e_dm ON tr.DepartmentManagerId = e_dm.Email
                    LEFT JOIN Employees e_ha ON tr.HRDAdminid = e_ha.Email
                    LEFT JOIN Employees e_hc ON tr.HRDConfirmationid = e_hc.Email
                    LEFT JOIN Employees e_md ON tr.ManagingDirectorId = e_md.Email
                    LEFT JOIN Employees e_dmd ON tr.DeputyManagingDirectorId = e_dmd.Email
                    WHERE tr.Id = @Id AND tr.IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            data.DocNo = reader["DocNo"]?.ToString();
                            data.Company = reader["Company"]?.ToString();
                            data.TrainingType = reader["TrainingType"]?.ToString();
                            data.Factory = reader["Factory"]?.ToString();
                            data.Department = reader["Department"]?.ToString();
                            data.Position = reader["Position"]?.ToString();
                            data.SeminarTitle = reader["SeminarTitle"]?.ToString();
                            data.TrainingLocation = reader["TrainingLocation"]?.ToString();
                            data.Instructor = reader["Instructor"]?.ToString();
                            data.StartDate = reader["StartDate"] != DBNull.Value ? (DateTime?)reader["StartDate"] : null;
                            data.EndDate = reader["EndDate"] != DBNull.Value ? (DateTime?)reader["EndDate"] : null;
                            data.TotalPeople = reader["TotalPeople"] != DBNull.Value ? (int)reader["TotalPeople"] : 0;
                            data.PerPersonTrainingHours = reader["PerPersonTrainingHours"] != DBNull.Value ? (int)reader["PerPersonTrainingHours"] : 0;
                            data.RegistrationCost = reader["RegistrationCost"] != DBNull.Value ? (decimal)reader["RegistrationCost"] : 0;
                            data.InstructorFee = reader["InstructorFee"] != DBNull.Value ? (decimal)reader["InstructorFee"] : 0;
                            data.EquipmentCost = reader["EquipmentCost"] != DBNull.Value ? (decimal)reader["EquipmentCost"] : 0;
                            data.FoodCost = reader["FoodCost"] != DBNull.Value ? (decimal)reader["FoodCost"] : 0;
                            data.OtherCost = reader["OtherCost"] != DBNull.Value ? (decimal)reader["OtherCost"] : 0;
                            data.OtherCostDescription = reader["OtherCostDescription"]?.ToString();
                            data.TotalCost = reader["TotalCost"] != DBNull.Value ? (decimal)reader["TotalCost"] : 0;
                            data.CostPerPerson = reader["CostPerPerson"] != DBNull.Value ? (decimal)reader["CostPerPerson"] : 0;
                            data.TrainingObjective = reader["TrainingObjective"]?.ToString();
                            data.OtherObjective = reader["OtherObjective"]?.ToString();
                            data.ExpectedOutcome = reader["ExpectedOutcome"]?.ToString();
                            data.TravelMethod = reader["TravelMethod"]?.ToString();
                            data.TargetGroup = reader["TargetGroup"]?.ToString();
                            data.Status = reader["Status"]?.ToString();
                            data.CreatedBy = reader["CreatedBy"]?.ToString();
                            data.CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime)reader["CreatedDate"] : DateTime.Now;
                            data.SectionManagerId = reader["SectionManagerId"]?.ToString();
                            data.Status_SectionManager = reader["Status_SectionManager"]?.ToString();
                            data.ApproveInfo_SectionManager = reader["ApproveInfo_SectionManager"]?.ToString();
                            data.DepartmentManagerId = reader["DepartmentManagerId"]?.ToString();
                            data.Status_DepartmentManager = reader["Status_DepartmentManager"]?.ToString();
                            data.ApproveInfo_DepartmentManager = reader["ApproveInfo_DepartmentManager"]?.ToString();
                            data.HRDAdminId = reader["HRDAdminId"]?.ToString();
                            data.Status_HRDAdmin = reader["Status_HRDAdmin"]?.ToString();
                            data.ApproveInfo_HRDAdmin = reader["ApproveInfo_HRDAdmin"]?.ToString();
                            data.HRDConfirmationId = reader["HRDConfirmationId"]?.ToString();
                            data.Status_HRDConfirmation = reader["Status_HRDConfirmation"]?.ToString();
                            data.ApproveInfo_HRDConfirmation = reader["ApproveInfo_HRDConfirmation"]?.ToString();
                            data.ManagingDirectorId = reader["ManagingDirectorId"]?.ToString();
                            data.Status_ManagingDirector = reader["Status_ManagingDirector"]?.ToString();
                            data.ApproveInfo_ManagingDirector = reader["ApproveInfo_ManagingDirector"]?.ToString();
                            data.DeputyManagingDirectorId = reader["DeputyManagingDirectorId"]?.ToString();
                            data.Status_DeputyManagingDirector = reader["Status_DeputyManagingDirector"]?.ToString();
                            data.ApproveInfo_DeputyManagingDirector = reader["ApproveInfo_DeputyManagingDirector"]?.ToString();
                            data.SectionManagerLevel = reader["SectionManagerLevel"]?.ToString();
                            data.DepartmentManagerLevel = reader["DepartmentManagerLevel"]?.ToString();
                            data.HRDAdminLevel = reader["HRDAdminLevel"]?.ToString();
                            data.HRDConfirmationLevel = reader["HRDConfirmationLevel"]?.ToString();
                            data.ManagingDirectorLevel = reader["ManagingDirectorLevel"]?.ToString();
                            data.DeputyManagingDirectorLevel = reader["DeputyManagingDirectorLevel"]?.ToString();
                            // Knowledge Management Fields
                            data.KM_SubmitDocument = reader["KM_SubmitDocument"] != DBNull.Value ? (bool?)reader["KM_SubmitDocument"] : null;
                            data.KM_CreateReport = reader["KM_CreateReport"] != DBNull.Value ? (bool?)reader["KM_CreateReport"] : null;
                            data.KM_CreateReportDate = reader["KM_CreateReportDate"] != DBNull.Value ? (DateTime?)reader["KM_CreateReportDate"] : null;
                            data.KM_KnowledgeSharing = reader["KM_KnowledgeSharing"] != DBNull.Value ? (bool?)reader["KM_KnowledgeSharing"] : null;
                            data.KM_KnowledgeSharingDate = reader["KM_KnowledgeSharingDate"] != DBNull.Value ? (DateTime?)reader["KM_KnowledgeSharingDate"] : null;
                            // HRD Record Fields
                            data.HRD_ContactDate = reader["HRD_ContactDate"] != DBNull.Value ? (DateTime?)reader["HRD_ContactDate"] : null;
                            data.HRD_ContactPerson = reader["HRD_ContactPerson"]?.ToString();
                            data.HRD_PaymentDate = reader["HRD_PaymentDate"] != DBNull.Value ? (DateTime?)reader["HRD_PaymentDate"] : null;
                            data.HRD_PaymentMethod = reader["HRD_PaymentMethod"]?.ToString();
                            data.HRD_RecorderSignature = reader["HRD_RecorderSignature"]?.ToString();
                            // HRD Budget & Membership Fields
                            data.HRD_BudgetPlan = reader["HRD_BudgetPlan"]?.ToString();
                            data.HRD_BudgetUsage = reader["HRD_BudgetUsage"]?.ToString();
                            data.HRD_DepartmentBudgetRemaining = reader["HRD_DepartmentBudgetRemaining"] != DBNull.Value ? (decimal?)reader["HRD_DepartmentBudgetRemaining"] : null;
                            data.HRD_MembershipType = reader["HRD_MembershipType"]?.ToString();
                            data.HRD_MembershipCost = reader["HRD_MembershipCost"] != DBNull.Value ? (decimal?)reader["HRD_MembershipCost"] : null;
                            // HRD Section 4 Fields
                            data.HRD_TrainingRecord = reader["HRD_TrainingRecord"] != DBNull.Value ? (bool?)reader["HRD_TrainingRecord"] : null;
                            data.HRD_KnowledgeManagementDone = reader["HRD_KnowledgeManagementDone"] != DBNull.Value ? (bool?)reader["HRD_KnowledgeManagementDone"] : null;
                            data.HRD_CourseCertification = reader["HRD_CourseCertification"] != DBNull.Value ? (bool?)reader["HRD_CourseCertification"] : null;
                            // Comment Fields
                            data.Comment_DeputyManagingDirector = reader["Comment_DeputyManagingDirector"]?.ToString();
                        }
                    }
                }

                string employeeQuery = @"
                    SELECT EmployeeName, EmployeeCode, Level
                    FROM TrainingRequestEmployees
                    WHERE TrainingRequestId = @Id
                    ORDER BY Id";

                using (SqlCommand cmd = new SqlCommand(employeeQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            data.Employees.Add(new EmployeeData
                            {
                                EmployeeName = reader["EmployeeName"]?.ToString(),
                                EmployeeCode = reader["EmployeeCode"]?.ToString(),
                                Level = reader["Level"]?.ToString()
                            });
                        }
                    }
                }

                // Fetch Training History
                string historyQuery = @"
                    SELECT EmployeeCode, EmployeeName, HistoryType, TrainingDate, CourseName
                    FROM TrainingHistory
                    WHERE TrainingRequestId = @Id
                    ORDER BY Id";

                using (SqlCommand cmd = new SqlCommand(historyQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            data.TrainingHistories.Add(new TrainingHistoryPdfData
                            {
                                EmployeeCode = reader["EmployeeCode"]?.ToString(),
                                EmployeeName = reader["EmployeeName"]?.ToString(),
                                HistoryType = reader["HistoryType"]?.ToString(),
                                TrainingDate = reader["TrainingDate"] != DBNull.Value ? (DateTime?)reader["TrainingDate"] : null,
                                CourseName = reader["CourseName"]?.ToString()
                            });
                        }
                    }
                }
            }

            return data;
        }

        // ==========================================
        // DATA CLASSES
        // ==========================================

        private class TrainingRequestData
        {
            public string DocNo { get; set; }
            public string Company { get; set; }
            public string TrainingType { get; set; }
            public string Factory { get; set; }
            public string Department { get; set; }
            public string Position { get; set; }
            public string SeminarTitle { get; set; }
            public string TrainingLocation { get; set; }
            public string Instructor { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int TotalPeople { get; set; }
            public int PerPersonTrainingHours { get; set; }
            public decimal RegistrationCost { get; set; }
            public decimal InstructorFee { get; set; }
            public decimal EquipmentCost { get; set; }
            public decimal FoodCost { get; set; }
            public decimal OtherCost { get; set; }
            public string OtherCostDescription { get; set; }
            public decimal TotalCost { get; set; }
            public decimal CostPerPerson { get; set; }
            public string TrainingObjective { get; set; }
            public string OtherObjective { get; set; }
            public string ExpectedOutcome { get; set; }

            // การเดินทาง และ กลุ่มเป้าหมาย
            public string TravelMethod { get; set; }
            public string TargetGroup { get; set; }

            public string Status { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }

            public string SectionManagerId { get; set; }
            public string Status_SectionManager { get; set; }
            public string ApproveInfo_SectionManager { get; set; }
            public string DepartmentManagerId { get; set; }
            public string Status_DepartmentManager { get; set; }
            public string ApproveInfo_DepartmentManager { get; set; }
            public string HRDAdminId { get; set; }
            public string Status_HRDAdmin { get; set; }
            public string ApproveInfo_HRDAdmin { get; set; }
            public string HRDConfirmationId { get; set; }
            public string Status_HRDConfirmation { get; set; }
            public string ApproveInfo_HRDConfirmation { get; set; }
            public string ManagingDirectorId { get; set; }
            public string Status_ManagingDirector { get; set; }
            public string ApproveInfo_ManagingDirector { get; set; }

            // Deputy Managing Director (รองกรรมการผู้จัดการ)
            public string DeputyManagingDirectorId { get; set; }
            public string Status_DeputyManagingDirector { get; set; }
            public string ApproveInfo_DeputyManagingDirector { get; set; }

            public string SectionManagerLevel { get; set; }
            public string DepartmentManagerLevel { get; set; }
            public string HRDAdminLevel { get; set; }
            public string HRDConfirmationLevel { get; set; }
            public string ManagingDirectorLevel { get; set; }
            public string DeputyManagingDirectorLevel { get; set; }

            // Knowledge Management Fields
            public bool? KM_SubmitDocument { get; set; }
            public bool? KM_CreateReport { get; set; }
            public DateTime? KM_CreateReportDate { get; set; }
            public bool? KM_KnowledgeSharing { get; set; }
            public DateTime? KM_KnowledgeSharingDate { get; set; }

            // HRD Record Fields
            public DateTime? HRD_ContactDate { get; set; }
            public string HRD_ContactPerson { get; set; }
            public DateTime? HRD_PaymentDate { get; set; }
            public string HRD_PaymentMethod { get; set; }
            public string HRD_RecorderSignature { get; set; }

            // HRD Budget & Membership Fields
            public string HRD_BudgetPlan { get; set; }
            public string HRD_BudgetUsage { get; set; }
            public decimal? HRD_DepartmentBudgetRemaining { get; set; }
            public string HRD_MembershipType { get; set; }
            public decimal? HRD_MembershipCost { get; set; }

            // HRD Section 4: การดำเนินงานหลังอนุมัติ
            public bool? HRD_TrainingRecord { get; set; }
            public bool? HRD_KnowledgeManagementDone { get; set; }
            public bool? HRD_CourseCertification { get; set; }

            // Comment Fields
            public string Comment_DeputyManagingDirector { get; set; }

            public List<EmployeeData> Employees { get; set; } = new List<EmployeeData>();
            public List<TrainingHistoryPdfData> TrainingHistories { get; set; } = new List<TrainingHistoryPdfData>();
        }

        private class EmployeeData
        {
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string Level { get; set; }
        }

        private class TrainingHistoryPdfData
        {
            public string EmployeeCode { get; set; }
            public string EmployeeName { get; set; }
            public string HistoryType { get; set; }
            public DateTime? TrainingDate { get; set; }
            public string CourseName { get; set; }
        }
    }
}