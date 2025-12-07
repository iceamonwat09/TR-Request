using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// PDF Report Service - แบบฟอร์มคำขอฝึกอบรม (Training Request Form)
    /// ตรงตาม HTML Template 100%
    ///
    /// Version: 2.0 (Updated with 7 enhancements)
    /// </summary>
    public class PdfReportService : IPdfReportService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        // Fonts (Tahoma with Unicode support for Thai)
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

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Initialize fonts with Unicode support (Tahoma for Thai)
            var options = new XPdfFontOptions(PdfFontEncoding.Unicode);
            string fontName = "Tahoma";

            _fontTitle = new XFont(fontName, 14, XFontStyle.Bold, options);
            _fontHeader = new XFont(fontName, 10, XFontStyle.Bold, options);
            _fontNormal = new XFont(fontName, 9, XFontStyle.Regular, options);
            _fontSmall = new XFont(fontName, 8, XFontStyle.Regular, options);
            _fontBold = new XFont(fontName, 9, XFontStyle.Bold, options);
            _fontTiny = new XFont(fontName, 7, XFontStyle.Regular, options);

            // Colors and pens
            _grayBrush = new XSolidBrush(XColor.FromArgb(217, 217, 217)); // #d9d9d9
            _borderPen = new XPen(XColors.Black, 1);
            _thickPen = new XPen(XColors.Black, 2);
            _thinPen = new XPen(XColors.Black, 0.5);
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

                double margin = 20;
                double yPos = margin;
                double pageWidth = page.Width - (2 * margin);
                double vlabelWidth = 20; // Vertical label width

                // Main border (thick 2px)
                gfx.DrawRectangle(_thickPen, margin, yPos, pageWidth, page.Height - (2 * margin));

                // ==========================================
                // ส่วนที่ 1: ผู้ขอฝึกอบรมกรอก
                // ==========================================
                double section1Height = DrawSection1(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section1Height;

                // ==========================================
                // ส่วนที่ 2: ฝ่ายบุคคลกรอกรายละเอียดการบันทึกอบรม
                // ==========================================
                double section2Height = DrawSection2(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section2Height;

                // ==========================================
                // ส่วนที่ 3: การพิจารณาอนุมัติ
                // ==========================================
                double section3Height = DrawSection3(gfx, data, margin, yPos, pageWidth, vlabelWidth);

                // Save to memory
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
        // ส่วนที่ 1: ผู้ขอฝึกอบรมกรอก
        // ==========================================
        private double DrawSection1(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double startY = y;
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;

            // Vertical Label
            DrawVerticalLabel(gfx, x, y, vlabelWidth, 480, "ส่วนที่ 1 ผู้ขอฝึกอบรมกรอก");

            // === Row 1: ประเภทการอบรม ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("ประเภทการอบรม:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 100;
                DrawCheckbox(gfx, xPos, currentY + 4, data.TrainingType == "In-House");
                gfx.DrawString("อบรมภายใน (In-House Training)", _fontNormal, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 200;
                DrawCheckbox(gfx, xPos, currentY + 4, data.TrainingType == "Public");
                gfx.DrawString("อบรมภายนอก (Public Training)", _fontNormal, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
            });

            // === Row 2: สาขา ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("สาขา:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 50;
                DrawCheckbox(gfx, xPos, currentY + 4, data.Factory == "สมุทรสาคร");
                gfx.DrawString("สมุทรสาคร", _fontNormal, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 100;
                DrawCheckbox(gfx, xPos, currentY + 4, data.Factory == "ปราจีนบุรี");
                gfx.DrawString("ปราจีนบุรี", _fontNormal, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
            });

            // === Row 3: เรียน / สำเนาเรียน ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                double halfWidth = contentWidth / 2;

                // Left: เรียน
                gfx.DrawString("เรียน", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 35, currentY + 13, xPos + halfWidth - 10, currentY + 13);
                gfx.DrawString("ผู้จัดการฝ่ายทรัพยากรบุคคล", _fontSmall, XBrushes.Black, new XPoint(xPos + 38, currentY + 12));

                // Vertical separator
                gfx.DrawLine(_borderPen, contentX + halfWidth, currentY, contentX + halfWidth, currentY + 18);

                // Right: สำเนาเรียน
                xPos = contentX + halfWidth + 5;
                gfx.DrawString("สำเนาเรียน", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 65, currentY + 13, contentX + contentWidth - 5, currentY + 13);
            });

            // === Row 4: สิ่งที่แนบมาด้วย ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("สิ่งที่แนบมาด้วย:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 90, currentY + 13, contentX + contentWidth - 5, currentY + 13);
            });

            // === Row 5: ด้วยแผนก / ฝ่าย / มีความประสงค์จะ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("ด้วยแผนก", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 60, currentY + 13, xPos + 140, currentY + 13);
                gfx.DrawString(data.Department ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos + 63, currentY + 12));

                xPos += 150;
                gfx.DrawString("ฝ่าย", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 30, currentY + 13, xPos + 110, currentY + 13);

                xPos += 120;
                gfx.DrawString("มีความประสงค์จะ", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
            });

            // === Row 6: ขอฝึกอบรมหลักสูตร ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("ขอฝึกอบรมหลักสูตร:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 120, currentY + 13, contentX + contentWidth - 5, currentY + 13);
                gfx.DrawString(data.SeminarTitle ?? "", _fontNormal, XBrushes.Black, new XPoint(xPos + 123, currentY + 12));
            });

            // === Row 7: วัน/เวลา & ระยะเวลา ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                double halfWidth = contentWidth / 2;

                // Left: วัน/เวลาที่จัดอบรม
                gfx.DrawString("วัน/เวลาที่จัดอบรม:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 110, currentY + 13, xPos + halfWidth - 10, currentY + 13);
                string dateRange = "";
                if (data.StartDate.HasValue && data.EndDate.HasValue)
                    dateRange = $"{data.StartDate.Value:dd/MM/yyyy} - {data.EndDate.Value:dd/MM/yyyy}";
                gfx.DrawString(dateRange, _fontSmall, XBrushes.Black, new XPoint(xPos + 113, currentY + 12));

                // Vertical separator
                gfx.DrawLine(_borderPen, contentX + halfWidth, currentY, contentX + halfWidth, currentY + 18);

                // Right: ระยะเวลาอบรม
                xPos = contentX + halfWidth + 5;
                gfx.DrawString("ระยะเวลาอบรม:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 85;
                gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 30, currentY + 13);
                gfx.DrawString("วัน /", _fontSmall, XBrushes.Black, new XPoint(xPos + 35, currentY + 12));
                xPos += 50;
                gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 30, currentY + 13);
                gfx.DrawString(data.PerPersonTrainingHours.ToString(), _fontSmall, XBrushes.Black, new XPoint(xPos + 3, currentY + 12));
                gfx.DrawString("ชั่วโมง", _fontSmall, XBrushes.Black, new XPoint(xPos + 35, currentY + 12));
            });

            // === Row 8: สถานที่ & การเดินทาง ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                double halfWidth = contentWidth / 2;

                // Left: สถานที่
                gfx.DrawString("สถานที่:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 50;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("จัดรับส่ง", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 70;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("เดินทางเอง", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));

                // Vertical separator
                gfx.DrawLine(_borderPen, contentX + halfWidth, currentY, contentX + halfWidth, currentY + 18);

                // Right: การเดินทาง
                xPos = contentX + halfWidth + 5;
                gfx.DrawString("การเดินทาง:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 65, currentY + 13, contentX + contentWidth - 5, currentY + 13);
                gfx.DrawString(data.TrainingLocation ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos + 68, currentY + 12));
            });

            // === Row 9: โดยวิทยากร/สถาบัน ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("โดยวิทยากร/สถาบัน:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 120, currentY + 13, contentX + contentWidth - 5, currentY + 13);
                gfx.DrawString(data.Instructor ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos + 123, currentY + 12));
            });

            // === Row 10: กลุ่มเป้าหมาย & จำนวนผู้เข้ารับการอบรม ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                double halfWidth = contentWidth / 2;

                // Left: กลุ่มเป้าหมาย
                gfx.DrawString("กลุ่มเป้าหมาย:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 85, currentY + 13, xPos + halfWidth - 10, currentY + 13);
                gfx.DrawString(data.Position ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos + 88, currentY + 12));

                // Vertical separator
                gfx.DrawLine(_borderPen, contentX + halfWidth, currentY, contentX + halfWidth, currentY + 18);

                // Right: จำนวนผู้เข้ารับการอบรม
                xPos = contentX + halfWidth + 5;
                gfx.DrawString("จำนวนผู้เข้ารับการอบรม:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 145;
                gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 30, currentY + 13);
                gfx.DrawString(data.TotalPeople.ToString(), _fontSmall, XBrushes.Black, new XPoint(xPos + 3, currentY + 12));
                gfx.DrawString("คน", _fontSmall, XBrushes.Black, new XPoint(xPos + 35, currentY + 12));
            });

            // === Row 11-13: รายชื่อผู้เข้าอบรม (3 rows, แนวนอน) - ✅ FIX 1 ===
            currentY = DrawParticipantList(gfx, data, contentX, currentY, contentWidth);

            // === Row 14: วัตถุประสงค์ (6 ข้อ) - ✅ FIX 2 ===
            currentY = DrawObjectivesSection(gfx, data, contentX, currentY, contentWidth);

            // === Row 15: ผลที่คาดว่าจะได้รับ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("ผลที่คาดว่าจะได้รับ:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 120, currentY + 13, contentX + contentWidth - 5, currentY + 13);
                gfx.DrawString(data.ExpectedOutcome ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos + 123, currentY + 12));
            });

            // === Row 16: งบประมาณ - ✅ FIX 3 ===
            currentY = DrawBudgetSection(gfx, data, contentX, currentY, contentWidth);

            // === Row 17: จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ + ลายเซ็น - ✅ FIX 4 & 5 ===
            currentY = DrawSignatureSection(gfx, data, contentX, currentY, contentWidth);

            return currentY - startY;
        }

        // ==========================================
        // ส่วนที่ 2: ฝ่ายบุคคลกรอกรายละเอียดการบันทึกอบรม
        // ==========================================
        private double DrawSection2(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double startY = y;
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;

            // Vertical Label
            DrawVerticalLabel(gfx, x, y, vlabelWidth, 280, "ส่วนที่ 2 ฝ่ายบุคคลกรอกรายละเอียดการบันทึกอบรม");

            // Top border
            gfx.DrawLine(_thickPen, contentX, currentY, contentX + contentWidth, currentY);

            // === Row 1: การวางแผนงบประมาณ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("การวางแผนงบประมาณ:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 120;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("plan", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 50;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("Unplan", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
            });

            // === Row 2: การใช้งบประมาณ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("การใช้งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 100;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("ใช้งบประมาณตามแผน", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 130;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("ใช้งบต่ำสุด", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 80;
                gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 60, currentY + 13);
                gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 65, currentY + 12));
            });

            // === Row 3: การเป็นสมาชิก/สิทธิพิเศษ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("การเป็นสมาชิก/สิทธิพิเศษ:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 145;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 80;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("ไม่เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
            });

            // === Row 4: ประวัติการอบรม (คำอธิบาย) ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                gfx.DrawString("ประวัติการอบรม:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 90;
                string desc = "ตรวจสอบรายละเอียดประวัติการฝึกอบรมของพนักงานแต่ละราย การฝึกอบรมในหัวข้อ \"หลักสูตรเดียวกัน\" ที่เคยเรียนมาก่อน ได้";
                gfx.DrawString(desc, _fontTiny, XBrushes.Black, new XRect(xPos, currentY + 3, contentWidth - 95, 15), XStringFormats.TopLeft);
            });

            // === Row 5: ตารางประวัติการอบรม ===
            currentY = DrawTrainingHistoryTable(gfx, data, contentX, currentY, contentWidth);

            // === Row 6: ตรวจสอบโดย & อนุมัติผลการตรวจสอบ ===
            currentY = DrawFieldRow(gfx, contentX, currentY, contentWidth, () =>
            {
                double xPos = contentX + 5;
                double halfWidth = contentWidth / 2;

                // Left: ตรวจสอบโดย
                gfx.DrawString("ตรวจสอบโดย:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                gfx.DrawLine(_thinPen, xPos + 70, currentY + 13, xPos + halfWidth - 50, currentY + 13);
                gfx.DrawString("(   /   /   )", _fontSmall, XBrushes.Black, new XPoint(xPos + halfWidth - 45, currentY + 12));

                // Vertical separator
                gfx.DrawLine(_borderPen, contentX + halfWidth, currentY, contentX + halfWidth, currentY + 18);

                // Right: อนุมัติผลการตรวจสอบ
                xPos = contentX + halfWidth + 5;
                gfx.DrawString("อนุมัติผลการตรวจสอบ:", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 130;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
                xPos += 60;
                DrawCheckbox(gfx, xPos, currentY + 4, false);
                gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, currentY + 12));
            });

            return currentY - startY;
        }

        // ==========================================
        // ส่วนที่ 3: การพิจารณาอนุมัติ - ✅ FIX 6 & 7
        // ==========================================
        private double DrawSection3(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double startY = y;
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;

            // Vertical Label
            DrawVerticalLabel(gfx, x, y, vlabelWidth, 180, "ส่วนที่ 3 การพิจารณาอนุมัติ");

            // Top border
            gfx.DrawLine(_thickPen, contentX, currentY, contentX + contentWidth, currentY);

            // Two columns
            double halfWidth = contentWidth / 2;
            double leftX = contentX;
            double rightX = contentX + halfWidth;

            // Left & Right height tracking
            double leftY = currentY + 5;
            double rightY = currentY + 5;

            // === LEFT COLUMN: ผลการพิจารณา (Managing Director) - ✅ FIX 6 ===
            gfx.DrawString("ผลการพิจารณา :", _fontBold, XBrushes.Black, new XPoint(leftX + 5, leftY + 12));
            leftY += 20;

            // Check if Managing Director approved (case insensitive)
            bool isManagingApproved = data.Status_ManagingDirector?.ToUpper() == "APPROVED";

            DrawCheckbox(gfx, leftX + 10, leftY, isManagingApproved);
            gfx.DrawString("อนุมัติให้ฝึกอบรมสัมมนา", _fontSmall, XBrushes.Black, new XPoint(leftX + 25, leftY + 8));
            leftY += 15;

            DrawCheckbox(gfx, leftX + 10, leftY, !isManagingApproved);
            gfx.DrawString("ไม่อนุมัติ/ส่งกลับให้ต้นสังกัดทบทวนใหม่", _fontSmall, XBrushes.Black, new XPoint(leftX + 25, leftY + 8));
            leftY += 20;

            gfx.DrawString("เหตุผล", _fontBold, XBrushes.Black, new XPoint(leftX + 10, leftY + 8));
            gfx.DrawLine(_thinPen, leftX + 55, leftY + 9, leftX + halfWidth - 10, leftY + 9);
            leftY += 20;

            // Signature - Show Managing Director ID when APPROVED
            gfx.DrawString("ลงชื่อ", _fontSmall, XBrushes.Black, new XPoint(leftX + 10, leftY + 8));
            gfx.DrawLine(_thinPen, leftX + 45, leftY + 9, leftX + halfWidth - 10, leftY + 9);
            leftY += 15;
            gfx.DrawString("(", _fontSmall, XBrushes.Black, new XPoint(leftX + 30, leftY + 8));
            gfx.DrawLine(_thinPen, leftX + 40, leftY + 9, leftX + halfWidth - 20, leftY + 9);

            // ✅ Show Managing Director ID when APPROVED
            if (isManagingApproved)
            {
                gfx.DrawString(data.ManagingDirectorId ?? "", _fontSmall, XBrushes.Black, new XPoint(leftX + 45, leftY + 8));
            }

            gfx.DrawString(")", _fontSmall, XBrushes.Black, new XPoint(leftX + halfWidth - 15, leftY + 8));
            leftY += 15;
            gfx.DrawString("ตำแหน่ง", _fontSmall, XBrushes.Black, new XPoint(leftX + 10, leftY + 8));
            gfx.DrawLine(_thinPen, leftX + 55, leftY + 9, leftX + halfWidth - 10, leftY + 9);
            leftY += 20;

            // Vertical separator between columns
            gfx.DrawLine(_borderPen, rightX, currentY, rightX, currentY + 175);

            // === RIGHT COLUMN: ข้อมูลส่วน HRD บันทึกข้อมูล (HRD Confirmation) - ✅ FIX 7 ===
            gfx.DrawString("ข้อมูลส่วน HRD บันทึกข้อมูล", _fontBold, XBrushes.Black, new XPoint(rightX + 5, rightY + 12));
            rightY += 20;

            gfx.DrawString("- ติดต่อสถาบัน/ผู้สอน : วันที่", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, rightY + 8));
            gfx.DrawLine(_thinPen, rightX + 130, rightY + 9, rightX + halfWidth - 10, rightY + 9);
            rightY += 15;

            gfx.DrawString("- ชื่อผู้ที่ติดต่อด้วย", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, rightY + 8));
            gfx.DrawLine(_thinPen, rightX + 95, rightY + 9, rightX + halfWidth - 10, rightY + 9);
            rightY += 15;

            gfx.DrawString("- การชำระเงิน : ชำระเงินภายในวันที่", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, rightY + 8));
            gfx.DrawLine(_thinPen, rightX + 165, rightY + 9, rightX + halfWidth - 10, rightY + 9);
            rightY += 15;

            double cbX = rightX + 15;
            DrawCheckbox(gfx, cbX, rightY, false);
            gfx.DrawString("เช็ค/โอนเงินผ่านบัญชี", _fontSmall, XBrushes.Black, new XPoint(cbX + 15, rightY + 8));
            rightY += 12;

            DrawCheckbox(gfx, cbX, rightY, false);
            gfx.DrawString("ชำระเป็นเงินสด", _fontSmall, XBrushes.Black, new XPoint(cbX + 15, rightY + 8));
            rightY += 25;

            // ✅ Show HRD Confirmation ID when APPROVED (case insensitive)
            bool isHRDConfirmationApproved = data.Status_HRDConfirmation?.ToUpper() == "APPROVED";

            gfx.DrawString("ผู้บันทึก", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, rightY + 8));
            gfx.DrawLine(_thinPen, rightX + 50, rightY + 9, rightX + halfWidth - 10, rightY + 9);

            if (isHRDConfirmationApproved)
            {
                gfx.DrawString(data.HRDConfirmationId ?? "", _fontSmall, XBrushes.Black, new XPoint(rightX + 55, rightY + 8));
            }

            rightY += 20;

            // Bottom border
            double bottomY = Math.Max(leftY, rightY) + 5;
            gfx.DrawLine(_borderPen, contentX, bottomY, contentX + contentWidth, bottomY);

            return bottomY - startY;
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        private void DrawVerticalLabel(XGraphics gfx, double x, double y, double width, double height, string text)
        {
            // Draw rectangle with gray background
            gfx.DrawRectangle(_grayBrush, x, y, width, height);
            gfx.DrawRectangle(_thickPen, x, y, width, height);
            gfx.DrawLine(_borderPen, x + width, y, x + width, y + height);

            // Save graphics state
            XGraphicsState state = gfx.Save();

            // Rotate and draw text
            gfx.TranslateTransform(x + width / 2 + 3, y + height / 2);
            gfx.RotateTransform(-90);

            XSize textSize = gfx.MeasureString(text, _fontSmall);
            gfx.DrawString(text, _fontSmall, XBrushes.Black, new XPoint(-textSize.Width / 2, 0));

            // Restore graphics state
            gfx.Restore(state);
        }

        private double DrawFieldRow(XGraphics gfx, double x, double y, double width, Action drawContent)
        {
            double rowHeight = 18;

            // Draw row borders
            gfx.DrawLine(_borderPen, x, y, x + width, y); // Top
            gfx.DrawLine(_borderPen, x, y + rowHeight, x + width, y + rowHeight); // Bottom

            // Draw content
            drawContent();

            return y + rowHeight;
        }

        // ✅ FIX 1: Draw Participant List with real data from TrainingRequestEmployees
        private double DrawParticipantList(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double currentY = y;
            double rowHeight = 18;

            // Header row
            gfx.DrawLine(_borderPen, x, currentY, x + width, currentY);
            gfx.DrawLine(_borderPen, x, currentY + rowHeight, x + width, currentY + rowHeight);
            gfx.DrawString("รายชื่อ", _fontBold, XBrushes.Black, new XPoint(x + 5, currentY + 12));
            currentY += rowHeight;

            // 3 participant rows (or less if fewer employees)
            int maxRows = Math.Min(3, data.Employees.Count);

            for (int i = 0; i < 3; i++)
            {
                gfx.DrawLine(_borderPen, x, currentY + rowHeight, x + width, currentY + rowHeight);

                double xPos = x + 5;
                if (i == 0) xPos += 0; // First row has no indent
                else xPos += 50; // Other rows indent

                gfx.DrawString($"{i + 1}.", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                xPos += 15;

                // ✅ Show employee data if available
                if (i < data.Employees.Count)
                {
                    var employee = data.Employees[i];

                    // Employee Name
                    gfx.DrawString(employee.EmployeeName ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));

                    xPos += 160;
                    gfx.DrawString("รหัส", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                    xPos += 30;

                    // Employee Code
                    gfx.DrawString(employee.EmployeeCode ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));

                    xPos += 70;
                    gfx.DrawString("ตำแหน่ง", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                    xPos += 50;

                    // Level (NOT Position!)
                    gfx.DrawString(employee.Level ?? "", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                }
                else
                {
                    // Empty row with lines
                    gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 150, currentY + 13);

                    xPos += 160;
                    gfx.DrawString("รหัส", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                    xPos += 30;
                    gfx.DrawLine(_thinPen, xPos, currentY + 13, xPos + 60, currentY + 13);

                    xPos += 70;
                    gfx.DrawString("ตำแหน่ง", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + 12));
                    xPos += 50;
                    gfx.DrawLine(_thinPen, xPos, currentY + 13, x + width - 5, currentY + 13);
                }

                currentY += rowHeight;
            }

            return currentY;
        }

        // ✅ FIX 2: Draw Objectives with checkboxes based on TrainingObjective data
        private double DrawObjectivesSection(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double currentY = y;
            double rowHeight = 36; // Taller for 2 rows of checkboxes

            gfx.DrawLine(_borderPen, x, currentY, x + width, currentY);
            gfx.DrawLine(_borderPen, x, currentY + rowHeight, x + width, currentY + rowHeight);

            double xPos = x + 5;
            double yOffset = currentY + 10;

            gfx.DrawString("วัตถุประสงค์:", _fontBold, XBrushes.Black, new XPoint(xPos, yOffset));
            xPos += 85;

            // ✅ Check which objectives are selected (from TrainingObjective column)
            string objective = data.TrainingObjective ?? "";
            bool isObj1 = objective.Contains("พัฒนาทักษะ");
            bool isObj2 = objective.Contains("เพิ่มประสิทธิภาพ") || objective.Contains("คุณภาพ");
            bool isObj3 = objective.Contains("แก้ไข") || objective.Contains("ป้องกันปัญหา");
            bool isObj4 = objective.Contains("กฎหมาย") || objective.Contains("ข้อกำหนด");
            bool isObj5 = objective.Contains("ถ่ายทอดความรู้") || objective.Contains("ขยายผล");
            bool isObj6 = objective.Contains("อื่นๆ");

            // Row 1 - 3 objectives
            DrawCheckbox(gfx, xPos, yOffset - 6, isObj1);
            gfx.DrawString("พัฒนาทักษะความชำนาญ", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 145;

            DrawCheckbox(gfx, xPos, yOffset - 6, isObj2);
            gfx.DrawString("เพิ่มประสิทธิภาพ / คุณภาพ", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 165;

            DrawCheckbox(gfx, xPos, yOffset - 6, isObj3);
            gfx.DrawString("ช่วยแก้ไข / ป้องกันปัญหา", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));

            // Row 2 - 3 objectives
            yOffset += 15;
            xPos = x + 90;

            DrawCheckbox(gfx, xPos, yOffset - 6, isObj4);
            gfx.DrawString("กฎหมาย/ข้อกำหนด", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 145;

            DrawCheckbox(gfx, xPos, yOffset - 6, isObj5);
            gfx.DrawString("ถ่ายทอดความรู้/ขยายผลสู่อื่น", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 180;

            DrawCheckbox(gfx, xPos, yOffset - 6, isObj6);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 70;
            gfx.DrawLine(_thinPen, xPos, yOffset + 1, x + width - 5, yOffset + 1);

            // Show other objective text if specified
            if (isObj6 && !string.IsNullOrEmpty(data.OtherObjective))
            {
                gfx.DrawString(data.OtherObjective, _fontTiny, XBrushes.Black, new XPoint(xPos + 3, yOffset + 1));
            }

            return currentY + rowHeight;
        }

        // ✅ FIX 3: Draw Budget Section with all 5 items separated
        private double DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double currentY = y;
            double rowHeight = 48; // Taller for 3 rows

            gfx.DrawLine(_borderPen, x, currentY, x + width, currentY);
            gfx.DrawLine(_borderPen, x, currentY + rowHeight, x + width, currentY + rowHeight);

            double xPos = x + 5;
            double yOffset = currentY + 10;

            // Row 1: งบประมาณ
            gfx.DrawString("งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(xPos, yOffset));
            yOffset += 12;

            // Row 2: ค่าลงทะเบียน/วิทยากร, ค่าวิทยากร, ค่าอุปกรณ์
            xPos = x + 10;
            DrawCheckbox(gfx, xPos, yOffset - 6, data.RegistrationCost > 0);
            gfx.DrawString("ค่าลงทะเบียน/วิทยากร:", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 145;
            gfx.DrawString(data.RegistrationCost.ToString("N2"), _fontSmall, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 60, yOffset));

            xPos += 90;
            DrawCheckbox(gfx, xPos, yOffset - 6, data.InstructorFee > 0);
            gfx.DrawString("ค่าวิทยากร:", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 85;
            gfx.DrawString(data.InstructorFee.ToString("N2"), _fontSmall, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 60, yOffset));

            yOffset += 12;

            // Row 3: ค่าอุปกรณ์, ค่าอาหาร, อื่นๆ
            xPos = x + 10;
            DrawCheckbox(gfx, xPos, yOffset - 6, data.EquipmentCost > 0);
            gfx.DrawString("ค่าอุปกรณ์:", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 85;
            gfx.DrawString(data.EquipmentCost.ToString("N2"), _fontSmall, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 60, yOffset));

            xPos += 90;
            DrawCheckbox(gfx, xPos, yOffset - 6, data.FoodCost > 0);
            gfx.DrawString("ค่าอาหาร:", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 75;
            gfx.DrawString(data.FoodCost.ToString("N2"), _fontSmall, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 60, yOffset));

            xPos += 90;
            DrawCheckbox(gfx, xPos, yOffset - 6, data.OtherCost > 0);
            gfx.DrawString("อื่นๆ:", _fontSmall, XBrushes.Black, new XPoint(xPos + 15, yOffset));
            xPos += 50;
            gfx.DrawString(data.OtherCost.ToString("N2"), _fontSmall, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(xPos + 60, yOffset));

            yOffset += 12;

            // Row 4: รวมสุทธิ
            xPos = x + width - 180;
            gfx.DrawString("รวมสุทธิ:", _fontBold, XBrushes.Black, new XPoint(xPos, yOffset));
            xPos += 65;
            gfx.DrawString(data.TotalCost.ToString("N2"), _fontBold, XBrushes.Black, new XPoint(xPos, yOffset));
            gfx.DrawString("บาท", _fontBold, XBrushes.Black, new XPoint(xPos + 80, yOffset));

            return currentY + rowHeight;
        }

        // ✅ FIX 4 & 5: Draw Signature Section (Section Manager & Department Manager)
        private double DrawSignatureSection(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double currentY = y;
            double rowHeight = 100;

            // Top border (thick)
            gfx.DrawLine(_thickPen, x, currentY, x + width, currentY);

            // Center text
            gfx.DrawString("จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ", _fontBold, XBrushes.Black,
                new XRect(x, currentY + 8, width, 20), XStringFormats.TopCenter);

            currentY += 25;

            // Two columns
            double halfWidth = width / 2;
            double leftX = x;
            double rightX = x + halfWidth;

            // === LEFT: ต้นสังกัดทบทวน (Section Manager) - ✅ FIX 4 ===
            double leftY = currentY;
            gfx.DrawString("ต้นสังกัดทบทวน:", _fontBold, XBrushes.Black, new XPoint(leftX + 5, leftY));
            leftY += 15;

            // Check if Section Manager approved (case insensitive)
            bool isSectionApproved = data.Status_SectionManager?.ToUpper() == "APPROVED";

            DrawCheckbox(gfx, leftX + 10, leftY - 6, isSectionApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(leftX + 25, leftY));
            DrawCheckbox(gfx, leftX + 80, leftY - 6, !isSectionApproved);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(leftX + 95, leftY));
            leftY += 20;

            gfx.DrawString("ลงชื่อ", _fontSmall, XBrushes.Black, new XPoint(leftX + 10, leftY));
            gfx.DrawLine(_thinPen, leftX + 45, leftY + 1, leftX + halfWidth - 20, leftY + 1);
            leftY += 12;
            gfx.DrawString("(", _fontSmall, XBrushes.Black, new XPoint(leftX + 30, leftY));
            gfx.DrawLine(_thinPen, leftX + 40, leftY + 1, leftX + halfWidth - 30, leftY + 1);

            // ✅ Show Section Manager ID when APPROVED
            if (isSectionApproved)
            {
                gfx.DrawString(data.SectionManagerId ?? "", _fontSmall, XBrushes.Black, new XPoint(leftX + 45, leftY));
            }

            gfx.DrawString(")", _fontSmall, XBrushes.Black, new XPoint(leftX + halfWidth - 25, leftY));
            leftY += 12;
            gfx.DrawString("ตำแหน่ง", _fontSmall, XBrushes.Black, new XPoint(leftX + 10, leftY));
            gfx.DrawLine(_thinPen, leftX + 50, leftY + 1, leftX + halfWidth - 20, leftY + 1);

            // Vertical separator
            gfx.DrawLine(_borderPen, rightX, currentY - 25, rightX, currentY + 75);

            // === RIGHT: ต้นสังกัดทบทวน (Department Manager) - ✅ FIX 5 ===
            double rightY = currentY;

            // Check if Department Manager approved (case insensitive)
            bool isDepartmentApproved = data.Status_DepartmentManager?.ToUpper() == "APPROVED";

            DrawCheckbox(gfx, rightX + 10, rightY - 6, isDepartmentApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, rightY));
            DrawCheckbox(gfx, rightX + 80, rightY - 6, !isDepartmentApproved);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 95, rightY));
            rightY += 20;

            gfx.DrawString("ลงชื่อ", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, rightY));
            gfx.DrawLine(_thinPen, rightX + 45, rightY + 1, rightX + halfWidth - 20, rightY + 1);
            rightY += 12;
            gfx.DrawString("(", _fontSmall, XBrushes.Black, new XPoint(rightX + 30, rightY));
            gfx.DrawLine(_thinPen, rightX + 40, rightY + 1, rightX + halfWidth - 30, rightY + 1);

            // ✅ Show Department Manager ID when APPROVED
            if (isDepartmentApproved)
            {
                gfx.DrawString(data.DepartmentManagerId ?? "", _fontSmall, XBrushes.Black, new XPoint(rightX + 45, rightY));
            }

            gfx.DrawString(")", _fontSmall, XBrushes.Black, new XPoint(rightX + halfWidth - 25, rightY));
            rightY += 12;
            gfx.DrawString("ตำแหน่ง", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, rightY));
            gfx.DrawLine(_thinPen, rightX + 50, rightY + 1, rightX + halfWidth - 20, rightY + 1);

            // Bottom border
            currentY += rowHeight;
            gfx.DrawLine(_borderPen, x, currentY, x + width, currentY);

            return currentY;
        }

        private double DrawTrainingHistoryTable(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            double currentY = y;

            // Table header
            string[] headers = { "ที่", "รหัสพนักงาน", "ชื่อ - สกุล", "ไม่เคย", "เคย", "ใกล้เคียง", "เมื่อวันที่", "ชื่อหลักสูตร" };
            double[] colWidths = { 25, 70, 120, 35, 35, 50, 60, width - 395 };
            double rowHeight = 18;

            // Header row (gray background)
            double xCol = x;
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, currentY, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, currentY, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontTiny, XBrushes.Black,
                    new XRect(xCol, currentY + 5, colWidths[i], rowHeight), XStringFormats.TopCenter);
                xCol += colWidths[i];
            }

            currentY += rowHeight;

            // 3 data rows
            for (int row = 1; row <= 3; row++)
            {
                xCol = x;
                for (int col = 0; col < colWidths.Length; col++)
                {
                    gfx.DrawRectangle(_borderPen, xCol, currentY, colWidths[col], rowHeight);

                    if (col == 0) // Row number
                    {
                        gfx.DrawString(row.ToString(), _fontSmall, XBrushes.Black,
                            new XRect(xCol, currentY + 5, colWidths[col], rowHeight), XStringFormats.TopCenter);
                    }
                    else if (col >= 3 && col <= 5) // Checkbox columns
                    {
                        DrawCheckbox(gfx, xCol + (colWidths[col] / 2) - 5, currentY + 4, false);
                    }

                    xCol += colWidths[col];
                }
                currentY += rowHeight;
            }

            return currentY;
        }

        private void DrawCheckbox(XGraphics gfx, double x, double y, bool isChecked)
        {
            double size = 10;
            gfx.DrawRectangle(_borderPen, x, y, size, size);
            if (isChecked)
            {
                // Draw checkmark (✓)
                gfx.DrawLine(_borderPen, x + 2, y + 5, x + 4, y + 8);
                gfx.DrawLine(_borderPen, x + 4, y + 8, x + 8, y + 2);
            }
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

                // Query 1: Main Training Request data
                string query = @"
                    SELECT
                        DocNo, Company, TrainingType, Factory, Department, Position,
                        SeminarTitle, TrainingLocation, Instructor,
                        StartDate, EndDate, TotalPeople, PerPersonTrainingHours,
                        RegistrationCost, InstructorFee, EquipmentCost, FoodCost, OtherCost,
                        TotalCost, CostPerPerson,
                        TrainingObjective, OtherObjective, ExpectedOutcome,
                        Status, CreatedBy, CreatedDate,
                        SectionManagerId, Status_SectionManager, ApproveInfo_SectionManager,
                        DepartmentManagerId, Status_DepartmentManager, ApproveInfo_DepartmentManager,
                        HRDAdminid AS HRDAdminId, Status_HRDAdmin, ApproveInfo_HRDAdmin,
                        HRDConfirmationid AS HRDConfirmationId, Status_HRDConfirmation, ApproveInfo_HRDConfirmation,
                        ManagingDirectorId, Status_ManagingDirector, ApproveInfo_ManagingDirector
                    FROM TrainingRequests
                    WHERE Id = @Id AND IsActive = 1";

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
                            data.TotalCost = reader["TotalCost"] != DBNull.Value ? (decimal)reader["TotalCost"] : 0;
                            data.CostPerPerson = reader["CostPerPerson"] != DBNull.Value ? (decimal)reader["CostPerPerson"] : 0;
                            data.TrainingObjective = reader["TrainingObjective"]?.ToString();
                            data.OtherObjective = reader["OtherObjective"]?.ToString();
                            data.ExpectedOutcome = reader["ExpectedOutcome"]?.ToString();
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
                        }
                    }
                }

                // ✅ Query 2: Employees from TrainingRequestEmployees table
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
                                Level = reader["Level"]?.ToString() // ตำแหน่ง = Level
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
            public decimal TotalCost { get; set; }
            public decimal CostPerPerson { get; set; }
            public string TrainingObjective { get; set; }
            public string OtherObjective { get; set; }
            public string ExpectedOutcome { get; set; }
            public string Status { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }

            // Approvers - 5 levels
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

            // ✅ NEW: Employee List
            public List<EmployeeData> Employees { get; set; } = new List<EmployeeData>();
        }

        // ✅ NEW: Employee Data Class
        private class EmployeeData
        {
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string Level { get; set; } // ตำแหน่ง (NOT Position!)
        }
    }
}
