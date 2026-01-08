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
    ///
    /// Version: 3.5 (Section 3 Alignment)
    /// - v3.4: Section 2 จัด alignment มาตรฐาน
    /// - v3.5: Section 3 - ลงนาม/ApproveInfo จัดกึ่งกลาง, เส้นชิดหัวข้อมากขึ้น
    ///         - ใช้ MeasureString คำนวณตำแหน่งเส้นอัตโนมัติ
    ///         - labelToDataGap = 3px สำหรับทุก label
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

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

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

                // ส่วนที่ 1
                double section1Height = DrawSection1(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section1Height;

                // ส่วนที่ 2
                double section2Height = DrawSection2(gfx, data, margin, yPos, pageWidth, vlabelWidth);
                yPos += section2Height;

                // ส่วนที่ 3
                double section3Height = DrawSection3(gfx, data, margin, yPos, pageWidth, vlabelWidth);

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
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;
            double rowHeight = 18;
            double padding = 5;
            double textOffsetY = 12;

            // [FIX v2.9] เพิ่ม sectionHeight จาก 455 → 480 เพื่อให้ครอบคลุมส่วนลายเซ็นทั้งหมด
            double sectionHeight = 480;

            DrawVerticalLabel(gfx, x, y, vlabelWidth, sectionHeight, "ส่วนที่ 1 ผู้ร้องขอกรอกข้อมูล");
            gfx.DrawRectangle(_thickPen, contentX, y, contentWidth, sectionHeight);

            currentY = y + padding + 2;

            // === Row 1: ประเภทการอบรม ===
            double labelX = contentX + padding;
            double checkboxCol1 = contentX + 100;
            double checkboxCol2 = contentX + 290;

            gfx.DrawString("ประเภทการอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, data.TrainingType == "In-House");
            gfx.DrawString("อบรมภายใน (In-House Training)", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 4, data.TrainingType == "Public");
            gfx.DrawString("อบรมภายนอก (Public Training)", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 2: สาขา ===
            gfx.DrawString("สาขา :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, data.Factory == "สมุทรสาคร");
            gfx.DrawString("สมุทรสาคร", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol2, currentY + 4, data.Factory == "ปราจีนบุรี");
            gfx.DrawString("ปราจีนบุรี", _fontSmall, XBrushes.Black, new XPoint(checkboxCol2 + 14, currentY + textOffsetY));
            currentY += rowHeight;

            gfx.DrawLine(_thinPen, contentX, currentY, contentX + contentWidth, currentY);
            currentY += padding;

            // === Row 3: เรียน / สำเนาเรียน ===
            double halfWidth = contentWidth / 2;
            gfx.DrawString("เรียน :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 40, currentY + textOffsetY, 160, "ผู้จัดการฝ่ายทรัพยากรบุคคล", 3);

            double rightColX = contentX + halfWidth + padding;
            gfx.DrawString("สำเนาเรียน :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 68, currentY + textOffsetY + 3, halfWidth - 80);
            currentY += rowHeight;

            // === Row 4: สิ่งที่แนบมาด้วย ===
            gfx.DrawString("สิ่งที่แนบมาด้วย :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 95, currentY + textOffsetY + 3, contentWidth - 105);
            currentY += rowHeight;

            // === Row 5: ด้วยแผนก / ฝ่าย / มีความประสงค์จะ ===
            gfx.DrawString("ด้วยแผนก", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 55, currentY + textOffsetY, 110, data.Position ?? "", 3);

            double xPos = labelX + 175;
            gfx.DrawString("ฝ่าย", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
            DrawUnderlineText(gfx, xPos + 28, currentY + textOffsetY, 90, data.Department ?? "", 3);

            xPos += 130;
            gfx.DrawString("มีความประสงค์จะ", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 6: ขอฝึกอบรมหลักสูตร ===
            gfx.DrawString("ขอฝึกอบรมหลักสูตร:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 115, currentY + textOffsetY, contentWidth - 125, data.SeminarTitle ?? "", 3);
            currentY += rowHeight;

            // === Row 7: วัน/เวลา & ระยะเวลา ===
            gfx.DrawString("วัน/เวลาที่จัดอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            string dateRange = "";
            if (data.StartDate.HasValue && data.EndDate.HasValue)
                dateRange = $"{data.StartDate.Value:dd/MM/yyyy} - {data.EndDate.Value:dd/MM/yyyy}";
            DrawUnderlineText(gfx, labelX + 105, currentY + textOffsetY, 130, dateRange, 3);

            gfx.DrawString("รวมระยะเวลาการอบรม:", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            int workingDays = CalculateWorkingDays(data.StartDate, data.EndDate);
            DrawUnderlineText(gfx, rightColX + 125, currentY + textOffsetY, 25, workingDays.ToString(), 3);
            gfx.DrawString("วัน /", _fontSmall, XBrushes.Black, new XPoint(rightColX + 153, currentY + textOffsetY));
            DrawUnderlineText(gfx, rightColX + 180, currentY + textOffsetY, 25, data.PerPersonTrainingHours.ToString(), 3);
            gfx.DrawString("ชั่วโมง", _fontSmall, XBrushes.Black, new XPoint(rightColX + 208, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 8: สถานที่ & การเดินทาง ===
            gfx.DrawString("สถานที่ :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 50, currentY + textOffsetY, halfWidth - 70, data.TrainingLocation ?? "", 3);

            gfx.DrawString("การเดินทาง :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            double travelCbX = rightColX + 75;
            DrawCheckbox(gfx, travelCbX, currentY + 4, false);
            gfx.DrawString("จัดรถรับส่ง", _fontSmall, XBrushes.Black, new XPoint(travelCbX + 14, currentY + textOffsetY));
            travelCbX += 80;
            DrawCheckbox(gfx, travelCbX, currentY + 4, false);
            gfx.DrawString("เดินทางเอง", _fontSmall, XBrushes.Black, new XPoint(travelCbX + 14, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 9: โดยวิทยากร/สถาบัน ===
            gfx.DrawString("โดยวิทยากร/สถาบัน:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 115, currentY + textOffsetY, contentWidth - 125, data.Instructor ?? "", 3);
            currentY += rowHeight;

            // === Row 10: กลุ่มเป้าหมาย & จำนวนผู้เข้ารับการอบรม ===
            gfx.DrawString("กลุ่มเป้าหมาย :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderlineText(gfx, labelX + 82, currentY + textOffsetY, halfWidth - 100, "", 3);

            gfx.DrawString("จำนวนผู้เข้ารับการอบรม :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderlineText(gfx, rightColX + 130, currentY + textOffsetY, 30, data.TotalPeople.ToString(), 3);
            gfx.DrawString("คน", _fontSmall, XBrushes.Black, new XPoint(rightColX + 165, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 11-13: รายชื่อผู้เข้าอบรม ===
            currentY = DrawParticipantList(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY);

            // === Row 14: วัตถุประสงค์ ===
            currentY = DrawObjectivesSection(gfx, data, contentX, currentY, contentWidth, padding, textOffsetY);

            // === Row 15: ผลที่คาดว่าจะได้รับ ===
            gfx.DrawString("ผลที่คาดว่าจะได้รับ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
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
            double rowHeight = 18;
            double padding = 5;
            double textOffsetY = 12;
            double sectionHeight = 190; // [FIX v3.2] เพิ่มจาก 180 → 190 เพื่อให้ตำแหน่งไม่ติดเส้น

            DrawVerticalLabel(gfx, x, y, vlabelWidth, sectionHeight, "ส่วนที่ 2 ฝ่ายทรัพยากรบุคคลตรวจสอบ");
            gfx.DrawRectangle(_thickPen, contentX, y, contentWidth, sectionHeight);

            currentY = y + padding + 2;
            double labelX = contentX + padding;
            double checkboxCol1 = contentX + 130;

            // === Row 1: การวางแผนงบประมาณ ===
            gfx.DrawString("การวางแผนงบประมาณ :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1, currentY + 4, false);
            gfx.DrawString("plan", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 14, currentY + textOffsetY));
            DrawCheckbox(gfx, checkboxCol1 + 55, currentY + 4, false);
            gfx.DrawString("Unplan", _fontSmall, XBrushes.Black, new XPoint(checkboxCol1 + 69, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 2: การใช้งบประมาณ ===
            gfx.DrawString("การใช้งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            double cbX = labelX + 100;
            DrawCheckbox(gfx, cbX, currentY + 4, false);
            gfx.DrawString("ใช้งบประมาณตามแผน", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 130;
            DrawCheckbox(gfx, cbX, currentY + 4, false);
            gfx.DrawString("ใช้งบต้นสังกัด คงเหลือ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 125, currentY + textOffsetY + 3, 50);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 180, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 3: การเป็นสมาชิก/สิทธิพิเศษ ===
            gfx.DrawString("การเป็นสมาชิก/สิทธิพิเศษ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            cbX = labelX + 135;
            DrawCheckbox(gfx, cbX, currentY + 4, false);
            gfx.DrawString("เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 72, currentY + textOffsetY + 3, 40);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 117, currentY + textOffsetY));
            cbX += 150;
            DrawCheckbox(gfx, cbX, currentY + 4, false);
            gfx.DrawString("ไม่เป็นสมาชิก", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 82, currentY + textOffsetY + 3, 40);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 127, currentY + textOffsetY));
            currentY += rowHeight;

            // === Row 4: ประวัติการอบรม (คำอธิบาย) ===
            gfx.DrawString("ประวัติการอบรม :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            gfx.DrawString("จากการตรวจสอบประวัติการฝึกอบรมของพนักงานพบว่า (กรณีมีจำนวนมากให้แนบเอกสารเพิ่มได้)", _fontTiny, XBrushes.Black, new XPoint(labelX + 95, currentY + textOffsetY));
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
            double lineHeight = 18;

            // กำหนดระยะห่างมาตรฐาน
            double labelToDataGap = 5;  // ระยะห่างจาก label ถึงข้อมูล

            bool isHRDAdminApproved = data.Status_HRDAdmin?.ToUpper() == "APPROVED";
            bool isHRDConfirmationApproved = data.Status_HRDConfirmation?.ToUpper() == "APPROVED";

            // === Row 1: ตรวจสอบโดย / อนุมัติผลการตรวจสอบ ===
            // คอลัมน์ซ้าย
            string leftLabel1 = "ตรวจสอบโดย:";
            gfx.DrawString(leftLabel1, _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            XSize leftLabelSize1 = gfx.MeasureString(leftLabel1, _fontSmall);
            double leftDataX1 = labelX + leftLabelSize1.Width + labelToDataGap;
            double leftUnderlineWidth1 = halfWidth - leftLabelSize1.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, leftDataX1, currentY + textOffsetY + 3, leftUnderlineWidth1);

            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDAdmin))
            {
                gfx.DrawString(data.ApproveInfo_HRDAdmin, _fontTiny, XBrushes.Black, new XPoint(leftDataX1, currentY + textOffsetY + 1));
            }

            // คอลัมน์ขวา
            string rightLabel1 = "อนุมัติผลการตรวจสอบ:";
            gfx.DrawString(rightLabel1, _fontSmall, XBrushes.Black, new XPoint(rightColX + padding, currentY + textOffsetY));
            XSize rightLabelSize1 = gfx.MeasureString(rightLabel1, _fontSmall);
            double rightDataX1 = rightColX + padding + rightLabelSize1.Width + labelToDataGap;
            double rightUnderlineWidth1 = halfWidth - rightLabelSize1.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, rightDataX1, currentY + textOffsetY + 3, rightUnderlineWidth1);

            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDConfirmation))
            {
                gfx.DrawString(data.ApproveInfo_HRDConfirmation, _fontTiny, XBrushes.Black, new XPoint(rightDataX1, currentY + textOffsetY + 1));
            }
            currentY += lineHeight;

            // === Row 2: ตำแหน่ง (ใช้ระยะห่างเดียวกัน) ===
            // คอลัมน์ซ้าย
            string leftLabel2 = "ตำแหน่ง :";
            gfx.DrawString(leftLabel2, _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            XSize leftLabelSize2 = gfx.MeasureString(leftLabel2, _fontSmall);
            double leftDataX2 = labelX + leftLabelSize2.Width + labelToDataGap;
            double leftUnderlineWidth2 = halfWidth - leftLabelSize2.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, leftDataX2, currentY + textOffsetY + 3, leftUnderlineWidth2);

            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.HRDAdminLevel))
            {
                gfx.DrawString(data.HRDAdminLevel, _fontSmall, XBrushes.Black, new XPoint(leftDataX2, currentY + textOffsetY));
            }

            // คอลัมน์ขวา
            string rightLabel2 = "ตำแหน่ง :";
            gfx.DrawString(rightLabel2, _fontSmall, XBrushes.Black, new XPoint(rightColX + padding, currentY + textOffsetY));
            XSize rightLabelSize2 = gfx.MeasureString(rightLabel2, _fontSmall);
            double rightDataX2 = rightColX + padding + rightLabelSize2.Width + labelToDataGap;
            double rightUnderlineWidth2 = halfWidth - rightLabelSize2.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, rightDataX2, currentY + textOffsetY + 3, rightUnderlineWidth2);

            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.HRDConfirmationLevel))
            {
                gfx.DrawString(data.HRDConfirmationLevel, _fontSmall, XBrushes.Black, new XPoint(rightDataX2, currentY + textOffsetY));
            }

            return currentY + lineHeight + 3;
        }

        // ==========================================
        // [FIX v3.5] ส่วนที่ 3: การพิจารณาอนุมัติ - จัดกึ่งกลางและเส้นชิดหัวข้อ
        // ==========================================
        private double DrawSection3(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;
            double padding = 5;
            double textOffsetY = 11;
            double sectionHeight = 130;
            double halfWidth = contentWidth / 2;
            double labelToDataGap = 3; // ระยะห่างจาก label ถึงข้อมูล

            DrawVerticalLabel(gfx, x, y, vlabelWidth, sectionHeight, "ส่วนที่ 3 การพิจารณาอนุมัติ");
            gfx.DrawRectangle(_thickPen, contentX, y, contentWidth, sectionHeight);

            // เส้นแบ่งกลาง (แนวตั้ง)
            gfx.DrawLine(_borderPen, contentX + halfWidth, y, contentX + halfWidth, y + sectionHeight);

            // ===== LEFT: ผลการพิจารณา =====
            double leftX = contentX + padding;
            double leftY = y + padding + 2;
            double lineHeight = 15;

            gfx.DrawString("ผลการพิจารณา :", _fontBold, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            leftY += lineHeight + 3;

            bool isManagingApproved = data.Status_ManagingDirector?.ToUpper() == "APPROVED";
            bool isManagingRejected = data.Status_ManagingDirector?.ToUpper() == "REJECTED";

            DrawCheckbox(gfx, leftX + 5, leftY + 2, isManagingApproved);
            gfx.DrawString("อนุมัติให้ฝึกอบรมสัมมนา", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight;

            DrawCheckbox(gfx, leftX + 5, leftY + 2, isManagingRejected);
            gfx.DrawString("ไม่อนุมัติ/ส่งกลับให้ต้นสังกัดทบทวนใหม่", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight + 2;

            // เหตุผล - เส้นชิดหัวข้อ
            string reasonLabel = "เหตุผล :";
            gfx.DrawString(reasonLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize reasonLabelSize = gfx.MeasureString(reasonLabel, _fontSmall);
            double reasonDataX = leftX + reasonLabelSize.Width + labelToDataGap;
            DrawUnderline(gfx, reasonDataX, leftY + textOffsetY + 3, halfWidth - reasonLabelSize.Width - labelToDataGap - padding - 5);
            leftY += lineHeight + 2;

            // ลงนาม - เส้นชิดหัวข้อ + ข้อมูลกึ่งกลาง
            string signLabel = "ลงนาม:";
            gfx.DrawString(signLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize signLabelSize = gfx.MeasureString(signLabel, _fontSmall);
            double signDataX = leftX + signLabelSize.Width + labelToDataGap;
            double signUnderlineWidth = halfWidth - signLabelSize.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, signDataX, leftY + textOffsetY + 3, signUnderlineWidth);

            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorId))
            {
                // จัดกึ่งกลางในพื้นที่ underline
                XSize dataSize = gfx.MeasureString(data.ManagingDirectorId, _fontSmall);
                double centerX = signDataX + (signUnderlineWidth / 2) - (dataSize.Width / 2);
                gfx.DrawString(data.ManagingDirectorId, _fontSmall, XBrushes.Black, new XPoint(centerX, leftY + textOffsetY));
            }
            leftY += lineHeight;

            // ApproveInfo (วงเล็บ) - จัดกึ่งกลางในพื้นที่ underline
            if (isManagingApproved && !string.IsNullOrEmpty(data.ApproveInfo_ManagingDirector))
            {
                string approveText = $"( {data.ApproveInfo_ManagingDirector} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = signDataX + (signUnderlineWidth / 2) - (textSize.Width / 2);
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, leftY + textOffsetY - 3));
            }
            leftY += lineHeight - 4;

            // ตำแหน่ง - เส้นชิดหัวข้อ + ข้อมูลชิดซ้าย (ตามที่เคยบอก)
            string posLabel = "ตำแหน่ง :";
            gfx.DrawString(posLabel, _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            XSize posLabelSize = gfx.MeasureString(posLabel, _fontSmall);
            double posDataX = leftX + posLabelSize.Width + labelToDataGap;
            double posUnderlineWidth = halfWidth - posLabelSize.Width - labelToDataGap - padding - 5;
            DrawUnderline(gfx, posDataX, leftY + textOffsetY + 3, posUnderlineWidth);

            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorLevel))
            {
                gfx.DrawString(data.ManagingDirectorLevel, _fontSmall, XBrushes.Black, new XPoint(posDataX, leftY + textOffsetY));
            }

            // ===== RIGHT: ข้อมูลส่วน HRD บันทึกข้อมูล =====
            double rightX = contentX + halfWidth + padding;
            double rightY = y + padding + 2;

            gfx.DrawString("ข้อมูลส่วน HRD บันทึกข้อมูล", _fontBold, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            rightY += lineHeight + 3;

            // ติดต่อสถาบัน - เส้นชิดหัวข้อ
            string contactLabel = "- ติดต่อสถาบัน/ผู้สอน : วันที่";
            gfx.DrawString(contactLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize contactLabelSize = gfx.MeasureString(contactLabel, _fontSmall);
            DrawUnderline(gfx, rightX + contactLabelSize.Width + labelToDataGap, rightY + textOffsetY + 3, halfWidth - contactLabelSize.Width - labelToDataGap - padding - 5);
            // แสดงวันที่ติดต่อ
            if (data.HRD_ContactDate.HasValue)
            {
                string contactDate = data.HRD_ContactDate.Value.ToString("dd/MM/yyyy");
                gfx.DrawString(contactDate, _fontSmall, XBrushes.Black, new XPoint(rightX + contactLabelSize.Width + labelToDataGap, rightY + textOffsetY));
            }
            rightY += lineHeight;

            // ชื่อผู้ที่ติดต่อด้วย - เส้นชิดหัวข้อ
            string nameLabel = "  ชื่อผู้ที่ติดต่อด้วย";
            gfx.DrawString(nameLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize nameLabelSize = gfx.MeasureString(nameLabel, _fontSmall);
            DrawUnderline(gfx, rightX + nameLabelSize.Width + labelToDataGap, rightY + textOffsetY + 3, halfWidth - nameLabelSize.Width - labelToDataGap - padding - 5);
            // แสดงชื่อผู้ติดต่อ
            if (!string.IsNullOrEmpty(data.HRD_ContactPerson))
            {
                gfx.DrawString(data.HRD_ContactPerson, _fontSmall, XBrushes.Black, new XPoint(rightX + nameLabelSize.Width + labelToDataGap, rightY + textOffsetY));
            }
            rightY += lineHeight;

            // การชำระเงิน - เส้นชิดหัวข้อ
            string payLabel = "- การชำระเงิน : ชำระเงินภายในวันที่";
            gfx.DrawString(payLabel, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize payLabelSize = gfx.MeasureString(payLabel, _fontSmall);
            DrawUnderline(gfx, rightX + payLabelSize.Width + labelToDataGap, rightY + textOffsetY + 3, halfWidth - payLabelSize.Width - labelToDataGap - padding - 5);
            // แสดงวันที่ชำระเงิน
            if (data.HRD_PaymentDate.HasValue)
            {
                string payDate = data.HRD_PaymentDate.Value.ToString("dd/MM/yyyy");
                gfx.DrawString(payDate, _fontSmall, XBrushes.Black, new XPoint(rightX + payLabelSize.Width + labelToDataGap, rightY + textOffsetY));
            }
            rightY += lineHeight + 2;

            double cbX = rightX + 10;
            bool isCheck = data.HRD_PaymentMethod == "Check";
            bool isCash = data.HRD_PaymentMethod == "Cash";
            DrawCheckbox(gfx, cbX, rightY + 2, isCheck);
            gfx.DrawString("เช็ค/โอนเงินผ่านบัญชี", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, rightY + textOffsetY));
            rightY += lineHeight;

            DrawCheckbox(gfx, cbX, rightY + 2, isCash);
            gfx.DrawString("ชำระเป็นเงินสด", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, rightY + textOffsetY));
            rightY += lineHeight + 3;

            // ลงชื่อ - เส้นชิดหัวข้อ
            string signLabel2 = "ลงชื่อ :";
            gfx.DrawString(signLabel2, _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            XSize signLabel2Size = gfx.MeasureString(signLabel2, _fontSmall);
            DrawUnderline(gfx, rightX + signLabel2Size.Width + labelToDataGap, rightY + textOffsetY + 3, halfWidth - signLabel2Size.Width - labelToDataGap - padding - 60);

            // แสดงชื่อผู้บันทึก
            if (!string.IsNullOrEmpty(data.HRD_RecorderSignature))
            {
                gfx.DrawString(data.HRD_RecorderSignature, _fontSmall, XBrushes.Black, new XPoint(rightX + signLabel2Size.Width + labelToDataGap, rightY + textOffsetY));
            }
            gfx.DrawString("ผู้บันทึก", _fontSmall, XBrushes.Black, new XPoint(rightX + halfWidth - 55, rightY + textOffsetY));

            return sectionHeight;
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
            gfx.DrawString(text, _fontSmall, XBrushes.Black, new XPoint(-textSize.Width / 2, 0));

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
            gfx.DrawString(text ?? "", _fontSmall, XBrushes.Black, new XPoint(x, y));
            gfx.DrawLine(_dottedPen, x, y + lineOffset, x + width, y + lineOffset);
        }

        private double DrawParticipantList(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 17;
            double labelX = contentX + padding;

            gfx.DrawString("รายชื่อ", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            currentY += rowHeight;

            for (int i = 0; i < 3; i++)
            {
                double xPos = labelX + 25;
                gfx.DrawString($"{i + 1}.", _fontSmall, XBrushes.Black, new XPoint(labelX + 10, currentY + textOffsetY));

                if (i < data.Employees.Count)
                {
                    var emp = data.Employees[i];
                    DrawUnderlineText(gfx, xPos, currentY + textOffsetY, 145, emp.EmployeeName ?? "", 3);
                    xPos += 155;
                    gfx.DrawString("รหัส :", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderlineText(gfx, xPos + 35, currentY + textOffsetY, 60, emp.EmployeeCode ?? "", 3);
                    xPos += 105;
                    gfx.DrawString("ตำแหน่ง:", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderlineText(gfx, xPos + 50, currentY + textOffsetY, contentWidth - xPos - 55 + contentX, emp.Level ?? "", 3);
                }
                else
                {
                    DrawUnderline(gfx, xPos, currentY + textOffsetY + 3, 145);
                    xPos += 155;
                    gfx.DrawString("รหัส :", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderline(gfx, xPos + 35, currentY + textOffsetY + 3, 60);
                    xPos += 105;
                    gfx.DrawString("ตำแหน่ง:", _fontSmall, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
                    DrawUnderline(gfx, xPos + 50, currentY + textOffsetY + 3, contentWidth - xPos - 55 + contentX);
                }

                currentY += rowHeight;
            }

            return currentY;
        }

        private double DrawObjectivesSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 17;
            double labelX = contentX + padding;

            string objective = data.TrainingObjective ?? "";
            bool isObj1 = objective.Contains("พัฒนาทักษะ");
            bool isObj2 = objective.Contains("เพิ่มประสิทธิภาพ") || objective.Contains("คุณภาพ");
            bool isObj3 = objective.Contains("แก้ไข") || objective.Contains("ป้องกันปัญหา");
            bool isObj4 = objective.Contains("กฎหมาย") || objective.Contains("ข้อกำหนด");
            bool isObj5 = objective.Contains("ถ่ายทอดความรู้") || objective.Contains("ขยายผล");
            bool isObj6 = objective.Contains("อื่นๆ");

            gfx.DrawString("วัตถุประสงค์:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            // === Row 1: พัฒนาทักษะ + เพิ่มประสิทธิภาพ + ช่วยแก้ไข + อื่นๆ (ระบุ) ===
            double cbX = labelX + 75;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj1);
            gfx.DrawString("พัฒนาทักษะความชำนาญ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 125;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj2);
            gfx.DrawString("เพิ่มประสิทธิภาพ / คุณภาพ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 135;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj3);
            gfx.DrawString("ช่วยแก้ไข / ป้องกันปัญหา", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // ย้าย "อื่นๆ (ระบุ)" มาอยู่บรรทัดเดียวกับ "ช่วยแก้ไข / ป้องกันปัญหา"
            currentY += rowHeight;
            cbX = labelX + 75;

            // === Row 2: กฎหมาย + ถ่ายทอดความรู้ + อื่นๆ (ระบุ) ===
            DrawCheckbox(gfx, cbX, currentY + 4, isObj4);
            gfx.DrawString("กฎหมาย/ข้อกำหนดลูกค้า", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 125;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj5);
            gfx.DrawString("ถ่ายทอดความรู้/ขยายผลสู่ผู้อื่น", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX += 175;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj6);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 72, currentY + textOffsetY + 3, contentWidth - cbX - 77 + contentX);

            currentY += rowHeight;
            return currentY;
        }

        // ==========================================
        // [FIX v2.6] DrawBudgetSection - ปรับ alignment ให้ตรงกัน
        // ==========================================
        private double DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 17;
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

            gfx.DrawString("งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            // === แถว 1: ค่าลงทะเบียน + ค่าวิทยากร ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.RegistrationCost > 0);
            gfx.DrawString("ค่าลงทะเบียน", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.RegistrationCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.InstructorFee > 0);
            gfx.DrawString("ค่าวิทยากร", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, underlineWidth, data.InstructorFee.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col2BahtX, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 2: ค่าอาหาร + ค่าอุปกรณ์ ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.FoodCost > 0);
            gfx.DrawString("ค่าอาหาร", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.FoodCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.EquipmentCost > 0);
            gfx.DrawString("ค่าอุปกรณ์", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, underlineWidth, data.EquipmentCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col2BahtX, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 3: ค่าใช้จ่ายต่อคน + อื่นๆ (ระบุ) ===
            DrawCheckbox(gfx, col1X, currentY + 4, data.CostPerPerson > 0);
            gfx.DrawString("ค่าใช้จ่ายต่อคน", _fontSmall, XBrushes.Black, new XPoint(col1TextX, currentY + textOffsetY));
            DrawUnderlineText(gfx, col1ValueX, currentY + textOffsetY, underlineWidth, data.CostPerPerson.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col1BahtX, currentY + textOffsetY));

            DrawCheckbox(gfx, col2X, currentY + 4, data.OtherCost > 0);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(col2TextX, currentY + textOffsetY));

            // แสดง OtherCostDescription + OtherCost
            string otherText = "";
            if (!string.IsNullOrEmpty(data.OtherCostDescription))
                otherText = $"{data.OtherCostDescription} {data.OtherCost:N0}";
            else if (data.OtherCost > 0)
                otherText = data.OtherCost.ToString("N0");

            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, 100, otherText, 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col2ValueX + 105, currentY + textOffsetY));
            currentY += rowHeight;

            // === แถว 4: รวมสุทธิ (จัดให้อยู่ตำแหน่งเดียวกับคอลัมน์ขวา) ===
            gfx.DrawString("รวมสุทธิ:", _fontBold, XBrushes.Black, new XPoint(col2X, currentY + textOffsetY));
            DrawUnderlineText(gfx, col2ValueX, currentY + textOffsetY, underlineWidth + 10, data.TotalCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(col2ValueX + 65, currentY + textOffsetY));
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
        // [FIX v3.1] DrawSignatureSection - ApproveInfo กึ่งกลาง, ตำแหน่งชิดซ้าย
        // ==========================================
        private double DrawSignatureSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY, double sectionBottom)
        {
            double halfWidth = contentWidth / 2;
            double labelX = contentX + padding;
            double rightColX = contentX + halfWidth + padding;
            double lineHeight = 15;

            // บันทึกตำแหน่งเริ่มต้นสำหรับเส้นแบ่งกลาง
            double sigStartY = currentY;

            // === ROW 1: หัวข้อทั้งสองฝั่ง ===
            gfx.DrawString("จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            gfx.DrawString("ต้นสังกัดทบทวน :", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            currentY += lineHeight + 2;

            // === ROW 2: Checkbox อนุมัติ/ไม่อนุมัติ ===
            double cbX = labelX;
            bool isSectionApproved = data.Status_SectionManager?.ToUpper() == "APPROVED";
            bool isSectionRejected = data.Status_SectionManager?.ToUpper() == "REJECTED";
            DrawCheckbox(gfx, cbX, currentY + 4, isSectionApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 60;
            DrawCheckbox(gfx, cbX, currentY + 4, isSectionRejected);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            cbX = rightColX;
            bool isDepartmentApproved = data.Status_DepartmentManager?.ToUpper() == "APPROVED";
            bool isDepartmentRejected = data.Status_DepartmentManager?.ToUpper() == "REJECTED";
            DrawCheckbox(gfx, cbX, currentY + 4, isDepartmentApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 60;
            DrawCheckbox(gfx, cbX, currentY + 4, isDepartmentRejected);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            currentY += lineHeight + 1;

            // === ROW 3: ลงชื่อ (จัดกึ่งกลางในพื้นที่ underline) ===
            double underlineWidth = halfWidth - 55;

            // LEFT
            gfx.DrawString("ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 40, currentY + textOffsetY + 3, underlineWidth);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerId))
            {
                XSize textSize = gfx.MeasureString(data.SectionManagerId, _fontSmall);
                double centerX = labelX + 40 + (underlineWidth / 2) - (textSize.Width / 2);
                gfx.DrawString(data.SectionManagerId, _fontSmall, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY));
            }

            // RIGHT
            gfx.DrawString("ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 40, currentY + textOffsetY + 3, underlineWidth);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerId))
            {
                XSize textSize = gfx.MeasureString(data.DepartmentManagerId, _fontSmall);
                double centerX = rightColX + 40 + (underlineWidth / 2) - (textSize.Width / 2);
                gfx.DrawString(data.DepartmentManagerId, _fontSmall, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY));
            }
            currentY += lineHeight;

            // === ROW 4: ApproveInfo (วงเล็บ) - จัดกึ่งกลางในพื้นที่ underline ของ "ลงชื่อ" ===
            // LEFT
            if (isSectionApproved && !string.IsNullOrEmpty(data.ApproveInfo_SectionManager))
            {
                string approveText = $"( {data.ApproveInfo_SectionManager} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                // กึ่งกลางในพื้นที่ underline (เริ่มจาก labelX + 40)
                double centerX = labelX + 40 + (underlineWidth / 2) - (textSize.Width / 2);
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 5));
            }

            // RIGHT
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.ApproveInfo_DepartmentManager))
            {
                string approveText = $"( {data.ApproveInfo_DepartmentManager} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                // กึ่งกลางในพื้นที่ underline (เริ่มจาก rightColX + 40)
                double centerX = rightColX + 40 + (underlineWidth / 2) - (textSize.Width / 2);
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 5));
            }
            currentY += lineHeight - 3;

            // === ROW 5: ตำแหน่ง (ชิดซ้ายเหมือนเดิม) ===
            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerLevel))
            {
                gfx.DrawString(data.SectionManagerLevel, _fontSmall, XBrushes.Black, new XPoint(labelX + 52, currentY + textOffsetY));
            }

            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerLevel))
            {
                gfx.DrawString(data.DepartmentManagerLevel, _fontSmall, XBrushes.Black, new XPoint(rightColX + 52, currentY + textOffsetY));
            }

            // เส้นแบ่งกลาง (แนวตั้ง)
            gfx.DrawLine(_borderPen, contentX + halfWidth, sigStartY, contentX + halfWidth, currentY + lineHeight);

            return currentY + lineHeight;
        }

        private double DrawTrainingHistoryTable(XGraphics gfx, TrainingRequestData data, double x, double y, double width)
        {
            string[] headers = { "ที่", "รหัสพนักงาน", "ชื่อ - สกุล", "ไม่เคย", "เคย", "ใกล้เคียง", "เมื่อวันที่", "ชื่อหลักสูตร" };
            double[] colWidths = { 20, 65, 115, 35, 30, 45, 55, width - 365 };
            double rowHeight = 15;

            // Header row
            double xCol = x;
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, y, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, y, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontTiny, XBrushes.Black,
                    new XRect(xCol, y + 4, colWidths[i], rowHeight), XStringFormats.TopCenter);
                xCol += colWidths[i];
            }

            y += rowHeight;

            // Data rows
            for (int row = 1; row <= 3; row++)
            {
                xCol = x;
                for (int col = 0; col < colWidths.Length; col++)
                {
                    gfx.DrawRectangle(_borderPen, xCol, y, colWidths[col], rowHeight);

                    if (col == 0)
                    {
                        gfx.DrawString(row.ToString(), _fontTiny, XBrushes.Black,
                            new XRect(xCol, y + 4, colWidths[col], rowHeight), XStringFormats.TopCenter);
                    }
                    else if (col >= 3 && col <= 5)
                    {
                        double cbXPos = xCol + (colWidths[col] / 2) - 4;
                        double cbY = y + 3;
                        gfx.DrawRectangle(_thinPen, cbXPos, cbY, 8, 8);
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
                        tr.Status, tr.CreatedBy, tr.CreatedDate,
                        tr.SectionManagerId, tr.Status_SectionManager, tr.ApproveInfo_SectionManager,
                        tr.DepartmentManagerId, tr.Status_DepartmentManager, tr.ApproveInfo_DepartmentManager,
                        tr.HRDAdminid AS HRDAdminId, tr.Status_HRDAdmin, tr.ApproveInfo_HRDAdmin,
                        tr.HRDConfirmationid AS HRDConfirmationId, tr.Status_HRDConfirmation, tr.ApproveInfo_HRDConfirmation,
                        tr.ManagingDirectorId, tr.Status_ManagingDirector, tr.ApproveInfo_ManagingDirector,
                        tr.HRD_ContactDate, tr.HRD_ContactPerson, tr.HRD_PaymentDate, tr.HRD_PaymentMethod, tr.HRD_RecorderSignature,
                        e_sm.Level AS SectionManagerLevel,
                        e_dm.Level AS DepartmentManagerLevel,
                        e_ha.Level AS HRDAdminLevel,
                        e_hc.Level AS HRDConfirmationLevel,
                        e_md.Level AS ManagingDirectorLevel
                    FROM TrainingRequests tr
                    LEFT JOIN Employees e_sm ON tr.SectionManagerId = e_sm.Email
                    LEFT JOIN Employees e_dm ON tr.DepartmentManagerId = e_dm.Email
                    LEFT JOIN Employees e_ha ON tr.HRDAdminid = e_ha.Email
                    LEFT JOIN Employees e_hc ON tr.HRDConfirmationid = e_hc.Email
                    LEFT JOIN Employees e_md ON tr.ManagingDirectorId = e_md.Email
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
                            data.SectionManagerLevel = reader["SectionManagerLevel"]?.ToString();
                            data.DepartmentManagerLevel = reader["DepartmentManagerLevel"]?.ToString();
                            data.HRDAdminLevel = reader["HRDAdminLevel"]?.ToString();
                            data.HRDConfirmationLevel = reader["HRDConfirmationLevel"]?.ToString();
                            data.ManagingDirectorLevel = reader["ManagingDirectorLevel"]?.ToString();
                            // HRD Record Fields
                            data.HRD_ContactDate = reader["HRD_ContactDate"] != DBNull.Value ? (DateTime?)reader["HRD_ContactDate"] : null;
                            data.HRD_ContactPerson = reader["HRD_ContactPerson"]?.ToString();
                            data.HRD_PaymentDate = reader["HRD_PaymentDate"] != DBNull.Value ? (DateTime?)reader["HRD_PaymentDate"] : null;
                            data.HRD_PaymentMethod = reader["HRD_PaymentMethod"]?.ToString();
                            data.HRD_RecorderSignature = reader["HRD_RecorderSignature"]?.ToString();
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

            public string SectionManagerLevel { get; set; }
            public string DepartmentManagerLevel { get; set; }
            public string HRDAdminLevel { get; set; }
            public string HRDConfirmationLevel { get; set; }
            public string ManagingDirectorLevel { get; set; }

            // HRD Record Fields
            public DateTime? HRD_ContactDate { get; set; }
            public string HRD_ContactPerson { get; set; }
            public DateTime? HRD_PaymentDate { get; set; }
            public string HRD_PaymentMethod { get; set; }
            public string HRD_RecorderSignature { get; set; }

            public List<EmployeeData> Employees { get; set; } = new List<EmployeeData>();
        }

        private class EmployeeData
        {
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string Level { get; set; }
        }
    }
}