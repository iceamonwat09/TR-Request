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
    /// Version: 2.5 (UI Fix - เส้นใต้ตำแหน่ง, ApproveInfo กึ่งกลาง)
    /// - v2.4: เส้นใต้ลายเซ็นครบ
    /// - v2.5: เส้นแบ่งใต้ตำแหน่ง, ขยับลายเซ็นส่วน2, ApproveInfo กึ่งกลาง
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

            double sectionHeight = 420;

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
            DrawUnderlineText(gfx, labelX + 55, currentY + textOffsetY, 110, data.Department ?? "", 3);

            double xPos = labelX + 175;
            gfx.DrawString("ฝ่าย", _fontBold, XBrushes.Black, new XPoint(xPos, currentY + textOffsetY));
            DrawUnderline(gfx, xPos + 28, currentY + textOffsetY + 3, 90);

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
            DrawUnderline(gfx, rightColX + 125, currentY + textOffsetY + 3, 25);
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
            DrawUnderlineText(gfx, labelX + 82, currentY + textOffsetY, halfWidth - 100, data.Position ?? "", 3);

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
            double sectionHeight = 180; // เพิ่มอีกเล็กน้อยเพื่อให้ไม่ทับ

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
        // ส่วนที่ 2: ลายเซ็น (แก้ไข layout ให้เส้นไม่ทับ)
        // ==========================================
        private double DrawSection2Signatures(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY, double sectionBottom)
        {
            double halfWidth = contentWidth / 2;
            double labelX = contentX + padding;
            double rightColX = contentX + halfWidth + padding;
            double lineHeight = 17; // เพิ่มระยะห่างระหว่างบรรทัด

            bool isHRDAdminApproved = data.Status_HRDAdmin?.ToUpper() == "APPROVED";
            bool isHRDConfirmationApproved = data.Status_HRDConfirmation?.ToUpper() == "APPROVED";

            // === Row 1: ตรวจสอบโดย / อนุมัติผลการตรวจสอบ ===
            gfx.DrawString("ตรวจสอบโดย:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            // เส้นใต้สำหรับตรวจสอบโดย
            double underlineStartLeft = labelX + 75;
            double underlineWidthLeft = halfWidth - 90;
            DrawUnderline(gfx, underlineStartLeft, currentY + textOffsetY + 3, underlineWidthLeft);

            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDAdmin))
            {
                gfx.DrawString(data.ApproveInfo_HRDAdmin, _fontSmall, XBrushes.Black, new XPoint(underlineStartLeft, currentY + textOffsetY));
            }

            gfx.DrawString("อนุมัติผลการตรวจสอบ:", _fontBold, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));

            // เส้นใต้สำหรับอนุมัติผลการตรวจสอบ
            double underlineStartRight = rightColX + 118;
            double underlineWidthRight = halfWidth - 130;
            DrawUnderline(gfx, underlineStartRight, currentY + textOffsetY + 3, underlineWidthRight);

            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.ApproveInfo_HRDConfirmation))
            {
                gfx.DrawString(data.ApproveInfo_HRDConfirmation, _fontSmall, XBrushes.Black, new XPoint(underlineStartRight, currentY + textOffsetY));
            }
            currentY += lineHeight;

            // === Row 2: ตำแหน่ง ===
            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isHRDAdminApproved && !string.IsNullOrEmpty(data.HRDAdminLevel))
            {
                gfx.DrawString(data.HRDAdminLevel, _fontSmall, XBrushes.Black, new XPoint(labelX + 52, currentY + textOffsetY));
            }

            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isHRDConfirmationApproved && !string.IsNullOrEmpty(data.HRDConfirmationLevel))
            {
                gfx.DrawString(data.HRDConfirmationLevel, _fontSmall, XBrushes.Black, new XPoint(rightColX + 52, currentY + textOffsetY));
            }

            // เส้นแบ่ง section จะอยู่ที่ขอบ rectangle ด้านล่าง (ไม่ต้องวาดเพิ่ม)

            return currentY + lineHeight;
        }

        // ==========================================
        // ส่วนที่ 3: การพิจารณาอนุมัติ
        // ==========================================
        private double DrawSection3(XGraphics gfx, TrainingRequestData data, double x, double y, double width, double vlabelWidth)
        {
            double contentX = x + vlabelWidth;
            double contentWidth = width - vlabelWidth;
            double currentY = y;
            double padding = 5;
            double textOffsetY = 11;
            double sectionHeight = 130; // เพิ่มอีกเล็กน้อย
            double halfWidth = contentWidth / 2;

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

            DrawCheckbox(gfx, leftX + 5, leftY + 2, isManagingApproved);
            gfx.DrawString("อนุมัติให้ฝึกอบรมสัมมนา", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight;

            DrawCheckbox(gfx, leftX + 5, leftY + 2, !isManagingApproved && !string.IsNullOrEmpty(data.Status_ManagingDirector));
            gfx.DrawString("ไม่อนุมัติ/ส่งกลับให้ต้นสังกัดทบทวนใหม่", _fontSmall, XBrushes.Black, new XPoint(leftX + 20, leftY + textOffsetY));
            leftY += lineHeight + 2;

            gfx.DrawString("เหตุผล :", _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            DrawUnderline(gfx, leftX + 45, leftY + textOffsetY + 3, halfWidth - 60);
            leftY += lineHeight + 2;

            // ลงนาม พร้อมเส้นใต้
            gfx.DrawString("ลงนาม:", _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            DrawUnderline(gfx, leftX + 42, leftY + textOffsetY + 3, halfWidth - 60);
            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorId))
            {
                gfx.DrawString(data.ManagingDirectorId, _fontSmall, XBrushes.Black, new XPoint(leftX + 42, leftY + textOffsetY));
            }
            leftY += lineHeight;

            // ApproveInfo (วงเล็บ) - จัดกึ่งกลางใต้ลงนาม
            if (isManagingApproved && !string.IsNullOrEmpty(data.ApproveInfo_ManagingDirector))
            {
                string approveText = $"( {data.ApproveInfo_ManagingDirector} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = leftX + (halfWidth - padding) / 2 - textSize.Width / 2;
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, leftY + textOffsetY - 3));
            }
            leftY += lineHeight - 4;

            // ตำแหน่ง พร้อมเส้นใต้
            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(leftX, leftY + textOffsetY));
            DrawUnderline(gfx, leftX + 52, leftY + textOffsetY + 3, halfWidth - 70);
            if (isManagingApproved && !string.IsNullOrEmpty(data.ManagingDirectorLevel))
            {
                gfx.DrawString(data.ManagingDirectorLevel, _fontSmall, XBrushes.Black, new XPoint(leftX + 52, leftY + textOffsetY));
            }

            // ===== RIGHT: ข้อมูลส่วน HRD บันทึกข้อมูล =====
            double rightX = contentX + halfWidth + padding;
            double rightY = y + padding + 2;

            gfx.DrawString("ข้อมูลส่วน HRD บันทึกข้อมูล", _fontBold, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            rightY += lineHeight + 3;

            gfx.DrawString("- ติดต่อสถาบัน/ผู้สอน : วันที่", _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            DrawUnderline(gfx, rightX + 130, rightY + textOffsetY + 3, halfWidth - 145);
            rightY += lineHeight;

            gfx.DrawString("  ชื่อผู้ที่ติดต่อด้วย", _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            DrawUnderline(gfx, rightX + 88, rightY + textOffsetY + 3, halfWidth - 103);
            rightY += lineHeight;

            gfx.DrawString("- การชำระเงิน : ชำระเงินภายในวันที่", _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            DrawUnderline(gfx, rightX + 160, rightY + textOffsetY + 3, halfWidth - 175);
            rightY += lineHeight + 2;

            double cbX = rightX + 10;
            DrawCheckbox(gfx, cbX, rightY + 2, false);
            gfx.DrawString("เช็ค/โอนเงินผ่านบัญชี", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, rightY + textOffsetY));
            rightY += lineHeight;

            DrawCheckbox(gfx, cbX, rightY + 2, false);
            gfx.DrawString("ชำระเป็นเงินสด", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, rightY + textOffsetY));
            rightY += lineHeight + 3;

            gfx.DrawString("ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(rightX, rightY + textOffsetY));
            DrawUnderline(gfx, rightX + 42, rightY + textOffsetY + 3, halfWidth - 120);
            bool isHRDConfirmationApproved = data.Status_HRDConfirmation?.ToUpper() == "APPROVED";
            if (isHRDConfirmationApproved)
            {
                gfx.DrawString(data.HRDConfirmationId ?? "", _fontSmall, XBrushes.Black, new XPoint(rightX + 42, rightY + textOffsetY));
            }
            gfx.DrawString("ผู้บันทึก", _fontSmall, XBrushes.Black, new XPoint(rightX + halfWidth - 60, rightY + textOffsetY));

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

            double cbX = labelX + 75;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj1);
            gfx.DrawString("พัฒนาทักษะความชำนาญ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 140;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj2);
            gfx.DrawString("เพิ่มประสิทธิภาพ / คุณภาพ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 145;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj3);
            gfx.DrawString("ช่วยแก้ไข / ป้องกันปัญหา", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            currentY += rowHeight;
            cbX = labelX + 75;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj4);
            gfx.DrawString("กฎหมาย/ข้อกำหนดลูกค้า", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 140;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj5);
            gfx.DrawString("ถ่ายทอดความรู้/ขยายผลสู่ผู้อื่น", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 160;
            DrawCheckbox(gfx, cbX, currentY + 4, isObj6);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderline(gfx, cbX + 72, currentY + textOffsetY + 3, contentWidth - cbX - 77 + contentX);

            currentY += rowHeight;
            return currentY;
        }

        private double DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY)
        {
            double rowHeight = 17;
            double labelX = contentX + padding;

            gfx.DrawString("งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            double cbX = labelX + 68;
            DrawCheckbox(gfx, cbX, currentY + 4, data.RegistrationCost > 0);
            gfx.DrawString("ค่าลงทะเบียน/วิทยากร", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderlineText(gfx, cbX + 118, currentY + textOffsetY, 45, data.RegistrationCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 168, currentY + textOffsetY));

            cbX += 200;
            DrawCheckbox(gfx, cbX, currentY + 4, data.EquipmentCost > 0);
            gfx.DrawString("ค่าเอกสาร/อุปกรณ์", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            DrawUnderlineText(gfx, cbX + 102, currentY + textOffsetY, 45, data.EquipmentCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 152, currentY + textOffsetY));

            currentY += rowHeight;
            cbX = labelX + 68;
            DrawCheckbox(gfx, cbX, currentY + 4, data.OtherCost > 0 || data.FoodCost > 0 || data.InstructorFee > 0);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            string otherDesc = "";
            if (data.InstructorFee > 0) otherDesc += $"ค่าวิทยากร {data.InstructorFee:N0}, ";
            if (data.FoodCost > 0) otherDesc += $"ค่าอาหาร {data.FoodCost:N0}, ";
            if (data.OtherCost > 0) otherDesc += $"อื่นๆ {data.OtherCost:N0}";
            otherDesc = otherDesc.TrimEnd(',', ' ');

            DrawUnderlineText(gfx, cbX + 72, currentY + textOffsetY, 115, otherDesc, 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 192, currentY + textOffsetY));

            cbX += 225;
            gfx.DrawString("รวมสุทธิ:", _fontBold, XBrushes.Black, new XPoint(cbX, currentY + textOffsetY));
            DrawUnderlineText(gfx, cbX + 52, currentY + textOffsetY, 60, data.TotalCost.ToString("N0"), 3);
            gfx.DrawString("บาท", _fontSmall, XBrushes.Black, new XPoint(cbX + 117, currentY + textOffsetY));

            currentY += rowHeight;
            return currentY;
        }

        private double DrawSignatureSection(XGraphics gfx, TrainingRequestData data, double contentX, double currentY, double contentWidth, double padding, double textOffsetY, double sectionBottom)
        {
            double halfWidth = contentWidth / 2;
            double labelX = contentX + padding;
            double rightColX = contentX + halfWidth + padding;
            double lineHeight = 16;

            // === CENTER: จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ ===
            gfx.DrawString("จึงเรียนมาเพื่อโปรดพิจารณาอนุมัติ", _fontBold, XBrushes.Black,
                new XRect(contentX, currentY, contentWidth, 18), XStringFormats.TopCenter);
            currentY += 20;

            // === LEFT: ต้นสังกัดทบทวน (Section Manager) ===
            gfx.DrawString("ต้นสังกัดทบทวน :", _fontBold, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));

            double cbX = labelX + 100;
            bool isSectionApproved = data.Status_SectionManager?.ToUpper() == "APPROVED";
            DrawCheckbox(gfx, cbX, currentY + 4, isSectionApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 55;
            DrawCheckbox(gfx, cbX, currentY + 4, !isSectionApproved && !string.IsNullOrEmpty(data.Status_SectionManager));
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            // === RIGHT: อนุมัติ (Department Manager) ===
            cbX = rightColX;
            bool isDepartmentApproved = data.Status_DepartmentManager?.ToUpper() == "APPROVED";
            DrawCheckbox(gfx, cbX, currentY + 4, isDepartmentApproved);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));
            cbX += 55;
            DrawCheckbox(gfx, cbX, currentY + 4, !isDepartmentApproved && !string.IsNullOrEmpty(data.Status_DepartmentManager));
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(cbX + 14, currentY + textOffsetY));

            currentY += lineHeight;

            // ลงชื่อ LEFT พร้อมเส้นใต้
            gfx.DrawString("ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 40, currentY + textOffsetY + 3, halfWidth - 55);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerId))
            {
                gfx.DrawString(data.SectionManagerId, _fontSmall, XBrushes.Black, new XPoint(labelX + 40, currentY + textOffsetY));
            }

            // ลงชื่อ RIGHT พร้อมเส้นใต้
            gfx.DrawString("ลงชื่อ :", _fontSmall, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 40, currentY + textOffsetY + 3, halfWidth - 55);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerId))
            {
                gfx.DrawString(data.DepartmentManagerId, _fontSmall, XBrushes.Black, new XPoint(rightColX + 40, currentY + textOffsetY));
            }

            currentY += lineHeight;

            // ApproveInfo LEFT (วงเล็บ) - จัดกึ่งกลาง
            if (isSectionApproved && !string.IsNullOrEmpty(data.ApproveInfo_SectionManager))
            {
                string approveText = $"( {data.ApproveInfo_SectionManager} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = labelX + (halfWidth - padding) / 2 - textSize.Width / 2;
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 4));
            }

            // ApproveInfo RIGHT (วงเล็บ) - จัดกึ่งกลาง
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.ApproveInfo_DepartmentManager))
            {
                string approveText = $"( {data.ApproveInfo_DepartmentManager} )";
                XSize textSize = gfx.MeasureString(approveText, _fontTiny);
                double centerX = rightColX + (halfWidth - padding) / 2 - textSize.Width / 2;
                gfx.DrawString(approveText, _fontTiny, XBrushes.Black, new XPoint(centerX, currentY + textOffsetY - 4));
            }

            currentY += lineHeight - 4;

            // ตำแหน่ง LEFT พร้อมเส้นใต้
            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(labelX, currentY + textOffsetY));
            DrawUnderline(gfx, labelX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isSectionApproved && !string.IsNullOrEmpty(data.SectionManagerLevel))
            {
                gfx.DrawString(data.SectionManagerLevel, _fontSmall, XBrushes.Black, new XPoint(labelX + 52, currentY + textOffsetY));
            }

            // ตำแหน่ง RIGHT พร้อมเส้นใต้
            gfx.DrawString("ตำแหน่ง :", _fontSmall, XBrushes.Black, new XPoint(rightColX, currentY + textOffsetY));
            DrawUnderline(gfx, rightColX + 52, currentY + textOffsetY + 3, halfWidth - 65);
            if (isDepartmentApproved && !string.IsNullOrEmpty(data.DepartmentManagerLevel))
            {
                gfx.DrawString(data.DepartmentManagerLevel, _fontSmall, XBrushes.Black, new XPoint(rightColX + 52, currentY + textOffsetY));
            }

            // เส้นแบ่งกลาง (แนวตั้ง) สำหรับส่วนลายเซ็น
            double sigTopY = currentY - (lineHeight * 2) - 16;
            gfx.DrawLine(_borderPen, contentX + halfWidth, sigTopY, contentX + halfWidth, currentY + lineHeight + 3);

            return currentY + lineHeight + 5;
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
                        tr.TotalCost, tr.CostPerPerson,
                        tr.TrainingObjective, tr.OtherObjective, tr.ExpectedOutcome,
                        tr.Status, tr.CreatedBy, tr.CreatedDate,
                        tr.SectionManagerId, tr.Status_SectionManager, tr.ApproveInfo_SectionManager,
                        tr.DepartmentManagerId, tr.Status_DepartmentManager, tr.ApproveInfo_DepartmentManager,
                        tr.HRDAdminid AS HRDAdminId, tr.Status_HRDAdmin, tr.ApproveInfo_HRDAdmin,
                        tr.HRDConfirmationid AS HRDConfirmationId, tr.Status_HRDConfirmation, tr.ApproveInfo_HRDConfirmation,
                        tr.ManagingDirectorId, tr.Status_ManagingDirector, tr.ApproveInfo_ManagingDirector,
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