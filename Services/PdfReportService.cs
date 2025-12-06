using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// PDF Report Service - แบบฟอร์มคำขอฝึกอบรม (ตรงตาม Template 100%)
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
        private XPen _thinPen;

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Initialize fonts with Unicode support (Tahoma for Thai)
            var options = new XPdfFontOptions(PdfFontEncoding.Unicode);
            string fontName = "Tahoma";

            _fontTitle = new XFont(fontName, 16, XFontStyle.Bold, options);
            _fontHeader = new XFont(fontName, 11, XFontStyle.Bold, options);
            _fontNormal = new XFont(fontName, 9, XFontStyle.Regular, options);
            _fontSmall = new XFont(fontName, 8, XFontStyle.Regular, options);
            _fontBold = new XFont(fontName, 9, XFontStyle.Bold, options);
            _fontTiny = new XFont(fontName, 7, XFontStyle.Regular, options);

            // Colors and pens
            _grayBrush = new XSolidBrush(XColor.FromArgb(240, 240, 240));
            _borderPen = new XPen(XColors.Black, 1);
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

                double margin = 30;
                double yPos = margin;
                double pageWidth = page.Width - (2 * margin);

                // ==========================================
                // 1. TITLE
                // ==========================================
                DrawTitle(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 2. HEADER (ประเภท, สาขา, เรียน, etc.)
                // ==========================================
                DrawHeader(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 3. MAIN BODY (2 Columns)
                // ==========================================
                DrawMainBody(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 4. PARTICIPANT TABLE (รายชื่อผู้เข้าอบรม)
                // ==========================================
                DrawParticipantTable(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 5. OBJECTIVES (วัตถุประสงค์)
                // ==========================================
                DrawObjectives(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 6. BUDGET (งบประมาณ)
                // ==========================================
                DrawBudgetSection(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 7. TRAINING HISTORY TABLE (ประวัติการฝึกอบรม)
                // ==========================================
                DrawTrainingHistoryTable(gfx, data, margin, ref yPos, pageWidth);

                // Check if need new page
                if (yPos > page.Height - 250)
                {
                    page = document.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = margin;
                }

                // ==========================================
                // 8. FOOTER (ผลการพิจารณา + HRD)
                // ==========================================
                DrawFooterSection(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // 9. APPROVERS (5 ระดับ - แสดงเฉพาะ APPROVED)
                // ==========================================
                DrawApprovers(gfx, data, margin, ref yPos, pageWidth);

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
        // DRAW METHODS
        // ==========================================

        private void DrawTitle(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // Title: แบบฟอร์มคำขอฝึกอบรม / TRAINING REQUEST FORM
            var titleTh = "แบบฟอร์มคำขอฝึกอบรม";
            var titleEn = "TRAINING REQUEST FORM";

            var sizeTh = gfx.MeasureString(titleTh, _fontTitle);
            var sizeEn = gfx.MeasureString(titleEn, _fontNormal);

            gfx.DrawString(titleTh, _fontTitle, XBrushes.Black,
                new XPoint(x + (width - sizeTh.Width) / 2, yPos + 15));

            gfx.DrawString(titleEn, _fontNormal, XBrushes.Black,
                new XPoint(x + (width - sizeEn.Width) / 2, yPos + 32));

            yPos += 45;
        }

        private void DrawHeader(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            double startY = yPos;

            // Border around entire header section
            gfx.DrawRectangle(_borderPen, x, yPos, width, 110);

            yPos += 10;

            // Line 1: ประเภทการอบรม
            gfx.DrawString("ประเภทการอบรม:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));

            double cbX = x + 120;
            DrawCheckbox(gfx, cbX, yPos - 8, data.TrainingType == "In-House");
            gfx.DrawString("อบรมภายใน (In-House Training)", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, yPos));

            cbX += 230;
            DrawCheckbox(gfx, cbX, yPos - 8, data.TrainingType == "Public");
            gfx.DrawString("อบรมภายนอก (Public Training)", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, yPos));

            yPos += 15;

            // Line 2: สาขา
            gfx.DrawString("สาขา:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));

            cbX = x + 50;
            DrawCheckbox(gfx, cbX, yPos - 8, data.Factory == "สมุทรสาคร");
            gfx.DrawString("สมุทรสาคร", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, yPos));

            cbX += 100;
            DrawCheckbox(gfx, cbX, yPos - 8, data.Factory == "ปราจีนบุรี");
            gfx.DrawString("ปราจีนบุรี", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, yPos));

            yPos += 15;

            // Line 3: เรียน
            gfx.DrawString("เรียน:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));
            gfx.DrawLine(_thinPen, x + 40, yPos + 2, x + width - 5, yPos + 2);
            gfx.DrawString("ผู้จัดการฝ่ายทรัพยากรบุคคล", _fontSmall, XBrushes.Black, new XPoint(x + 45, yPos));

            yPos += 15;

            // Line 4: เรื่อง
            gfx.DrawString("เรื่อง:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));
            gfx.DrawLine(_thinPen, x + 40, yPos + 2, x + width - 5, yPos + 2);
            gfx.DrawString(data.SeminarTitle ?? "", _fontNormal, XBrushes.Black, new XPoint(x + 45, yPos));

            yPos += 15;

            // Line 5: สำเนาเรียน
            gfx.DrawString("สำเนาเรียน:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));
            gfx.DrawLine(_thinPen, x + 70, yPos + 2, x + width - 5, yPos + 2);

            yPos += 15;

            // Line 6: สิ่งที่แนบมาด้วย
            gfx.DrawString("สิ่งที่แนบมาด้วย:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos));
            gfx.DrawLine(_thinPen, x + 90, yPos + 2, x + width - 5, yPos + 2);

            yPos += 15;

            // Line 7: ด้วยแผนก
            gfx.DrawString($"ด้วยแผนก: {data.Department ?? ""}", _fontNormal, XBrushes.Black, new XPoint(x + 5, yPos));
            gfx.DrawString($"มีความประสงค์ขอให้พนักงานเข้าร่วมฝึกอบรมหลักสูตร", _fontNormal, XBrushes.Black, new XPoint(x + 200, yPos));

            yPos = startY + 115;
        }

        private void DrawMainBody(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // 2 Columns: Left 70%, Right 30%
            double leftWidth = width * 0.7;
            double rightWidth = width * 0.3 - 5;
            double rightX = x + leftWidth + 5;

            double startY = yPos;

            // === LEFT COLUMN ===
            // Header (gray background)
            gfx.DrawRectangle(_grayBrush, x, yPos, leftWidth, 18);
            gfx.DrawRectangle(_borderPen, x, yPos, leftWidth, 18);
            gfx.DrawString("รายละเอียดการฝึกอบรม", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos + 13));

            yPos += 18;

            // Left content area
            double yLeft = yPos;
            double leftContentHeight = 130;
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, leftContentHeight);

            yLeft += 12;
            gfx.DrawString($"ชื่อผู้ขอ/ฝ่าย/แผนก: {data.CreatedBy ?? ""} / {data.Department ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"ชื่อหลักสูตรฝึกอบรม: {data.SeminarTitle ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            string dateRange = "";
            if (data.StartDate.HasValue && data.EndDate.HasValue)
            {
                dateRange = $"{data.StartDate.Value:dd/MM/yyyy} - {data.EndDate.Value:dd/MM/yyyy}";
            }
            gfx.DrawString($"วันที่อบรม: {dateRange}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"สถานที่อบรม: {data.TrainingLocation ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"วิทยากร: {data.Instructor ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"จำนวนชั่วโมงฝึกอบรม/คน: {data.PerPersonTrainingHours} ชม.", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"จำนวนผู้เข้าร่วมอบรม: {data.TotalPeople} คน", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"เลขที่เอกสาร: {data.DocNo ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"บริษัท: {data.Company ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            // === RIGHT COLUMN ===
            yPos = startY;

            // Header
            gfx.DrawRectangle(_grayBrush, rightX, yPos, rightWidth, 18);
            gfx.DrawRectangle(_borderPen, rightX, yPos, rightWidth, 18);
            gfx.DrawString("เพื่อพิจารณา", _fontBold, XBrushes.Black, new XPoint(rightX + 5, yPos + 13));

            yPos += 18;

            // Right content area
            double yRight = yPos;
            gfx.DrawRectangle(_borderPen, rightX, yRight, rightWidth, leftContentHeight);

            yRight += 15;
            DrawCheckbox(gfx, rightX + 5, yRight - 8, false);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 20, yRight));

            yRight += 15;
            DrawCheckbox(gfx, rightX + 5, yRight - 8, false);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 20, yRight));

            yRight += 15;
            gfx.DrawString("เนื่องจาก:", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, yRight));

            yRight += 12;
            gfx.DrawLine(_thinPen, rightX + 5, yRight, rightX + rightWidth - 5, yRight);

            yRight += 12;
            gfx.DrawLine(_thinPen, rightX + 5, yRight, rightX + rightWidth - 5, yRight);

            yRight += 15;
            gfx.DrawString("ลงชื่อ:", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, yRight));
            gfx.DrawLine(_thinPen, rightX + 35, yRight + 2, rightX + rightWidth - 5, yRight + 2);

            yRight += 12;
            gfx.DrawString("วันที่:", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, yRight));
            gfx.DrawLine(_thinPen, rightX + 35, yRight + 2, rightX + rightWidth - 5, yRight + 2);

            yPos = startY + 18 + leftContentHeight + 5;
        }

        private void DrawParticipantTable(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // Title
            gfx.DrawString("รายชื่อผู้เข้าร่วมฝึกอบรม:", _fontBold, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 18;

            // Table headers
            double[] colWidths = { 50, 150, 150, width - 350 };
            string[] headers = { "ลำดับ", "รหัส", "ชื่อ-สกุล", "ตำแหน่ง" };

            double xCol = x;
            double rowHeight = 20;

            // Header row (gray)
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontBold, XBrushes.Black, new XPoint(xCol + 5, yPos + 14));
                xCol += colWidths[i];
            }

            yPos += rowHeight;

            // 3 data rows
            for (int row = 1; row <= 3; row++)
            {
                xCol = x;
                for (int col = 0; col < colWidths.Length; col++)
                {
                    gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[col], rowHeight);
                    if (col == 0)
                    {
                        gfx.DrawString($"{row}.", _fontNormal, XBrushes.Black, new XPoint(xCol + 5, yPos + 14));
                    }
                    xCol += colWidths[col];
                }
                yPos += rowHeight;
            }

            yPos += 10;
        }

        private void DrawObjectives(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("วัตถุประสงค์การฝึกอบรม:", _fontBold, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 18;

            // 6 checkboxes in 2 rows
            string[] objectives = {
                "เพื่อพัฒนาทักษะ ความรู้ ความสามารถ",
                "เพื่อเพิ่มประสิทธิภาพการทำงาน",
                "เพื่อรองรับการเปลี่ยนแปลงขององค์กร",
                "เพื่อให้ทันต่อกฎหมาย ระเบียบที่เปลี่ยนแปลง",
                "เพื่อเตรียมความพร้อมสำหรับตำแหน่งใหม่",
                "อื่นๆ (ระบุ)"
            };

            int objIndex = 0;
            for (int row = 0; row < 2; row++)
            {
                double cbX = x + 10;
                for (int col = 0; col < 3; col++)
                {
                    if (objIndex < objectives.Length)
                    {
                        DrawCheckbox(gfx, cbX, yPos - 8, false);
                        gfx.DrawString(objectives[objIndex], _fontSmall, XBrushes.Black, new XPoint(cbX + 15, yPos));
                        cbX += 180;
                        objIndex++;
                    }
                }
                yPos += 15;
            }

            yPos += 5;

            // วัตถุประสงค์อื่นๆ
            if (!string.IsNullOrEmpty(data.TrainingObjective))
            {
                gfx.DrawString($"รายละเอียด: {data.TrainingObjective}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yPos));
                yPos += 12;
            }

            // ผลที่คาดหวัง
            gfx.DrawString("ผลที่คาดหวังจากการฝึกอบรม:", _fontBold, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 15;
            gfx.DrawLine(_thinPen, x + 5, yPos, x + width - 5, yPos);
            yPos += 12;
            gfx.DrawLine(_thinPen, x + 5, yPos, x + width - 5, yPos);
            yPos += 15;

            if (!string.IsNullOrEmpty(data.ExpectedOutcome))
            {
                gfx.DrawString(data.ExpectedOutcome, _fontSmall, XBrushes.Black, new XPoint(x + 5, yPos - 25));
            }
        }

        private void DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("งบประมาณ:", _fontBold, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 18;

            // Budget breakdown
            gfx.DrawString($"1. ค่าลงทะเบียน/วิทยากร: {data.RegistrationCost + data.InstructorFee:N2} บาท", _fontSmall, XBrushes.Black, new XPoint(x + 10, yPos));
            yPos += 12;

            gfx.DrawString($"2. ค่าเอกสาร/อุปกรณ์: {data.EquipmentCost:N2} บาท", _fontSmall, XBrushes.Black, new XPoint(x + 10, yPos));
            yPos += 12;

            gfx.DrawString($"3. ค่าอาหาร/เครื่องดื่ม: {data.FoodCost:N2} บาท", _fontSmall, XBrushes.Black, new XPoint(x + 10, yPos));
            yPos += 12;

            gfx.DrawString($"4. ค่าใช้จ่ายอื่นๆ: {data.OtherCost:N2} บาท", _fontSmall, XBrushes.Black, new XPoint(x + 10, yPos));
            yPos += 12;

            gfx.DrawString($"รวมสุทธิ: {data.TotalCost:N2} บาท ({data.TotalPeople} คน × {data.CostPerPerson:N2} บาท/คน)", _fontBold, XBrushes.Black, new XPoint(x + 10, yPos));
            yPos += 20;
        }

        private void DrawTrainingHistoryTable(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("ประวัติการฝึกอบรม (หลักสูตรเดียวกันหรือใกล้เคียง):", _fontBold, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 18;

            // 7 columns table
            double[] colWidths = { 30, 80, 120, 50, 50, 80, 80, width - 490 };
            string[] headers = { "ที่", "รหัสพนักงาน", "ชื่อ-สกุล", "ไม่เคย", "เคย", "ใกล้เคียง", "เมื่อวันที่", "ชื่อหลักสูตร" };

            double xCol = x;
            double rowHeight = 20;

            // Header row
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontTiny, XBrushes.Black, new XPoint(xCol + 3, yPos + 13));
                xCol += colWidths[i];
            }

            yPos += rowHeight;

            // 3 data rows
            for (int row = 1; row <= 3; row++)
            {
                xCol = x;
                for (int col = 0; col < colWidths.Length; col++)
                {
                    gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[col], rowHeight);
                    if (col == 0)
                    {
                        gfx.DrawString($"{row}", _fontSmall, XBrushes.Black, new XPoint(xCol + 5, yPos + 14));
                    }
                    xCol += colWidths[col];
                }
                yPos += rowHeight;
            }

            yPos += 15;
        }

        private void DrawFooterSection(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // 2 columns: Left (ผลการพิจารณา) / Right (HRD)
            double leftWidth = width / 2 - 5;
            double rightX = x + width / 2 + 5;

            double startY = yPos;

            // === LEFT COLUMN: ผลการพิจารณา ===
            double yLeft = yPos;

            gfx.DrawRectangle(_grayBrush, x, yLeft, leftWidth, 18);
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, 18);
            gfx.DrawString("ผลการพิจารณา", _fontBold, XBrushes.Black, new XPoint(x + 5, yLeft + 13));

            yLeft += 18;

            double leftContentHeight = 110;
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, leftContentHeight);

            yLeft += 15;
            DrawCheckbox(gfx, x + 10, yLeft - 8, false);
            gfx.DrawString("อนุมัติให้ฝึกอบรมสัมมนา", _fontSmall, XBrushes.Black, new XPoint(x + 25, yLeft));

            yLeft += 15;
            DrawCheckbox(gfx, x + 10, yLeft - 8, false);
            gfx.DrawString("ไม่อนุมัติ/ส่งกลับ", _fontSmall, XBrushes.Black, new XPoint(x + 25, yLeft));

            yLeft += 15;
            gfx.DrawString("เหตุผล:", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));
            gfx.DrawLine(_thinPen, x + 50, yLeft + 2, x + leftWidth - 5, yLeft + 2);

            yLeft += 12;
            gfx.DrawLine(_thinPen, x + 10, yLeft, x + leftWidth - 5, yLeft);

            yLeft += 15;
            gfx.DrawString("ลงชื่อ:", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));
            gfx.DrawLine(_thinPen, x + 45, yLeft + 2, x + leftWidth - 5, yLeft + 2);

            yLeft += 12;
            gfx.DrawString("ตำแหน่ง:", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));
            gfx.DrawLine(_thinPen, x + 55, yLeft + 2, x + leftWidth - 5, yLeft + 2);

            yLeft += 12;
            gfx.DrawString("วันที่:", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));
            gfx.DrawLine(_thinPen, x + 45, yLeft + 2, x + leftWidth - 5, yLeft + 2);

            // === RIGHT COLUMN: HRD ===
            double yRight = yPos;

            gfx.DrawRectangle(_grayBrush, rightX, yRight, leftWidth, 18);
            gfx.DrawRectangle(_borderPen, rightX, yRight, leftWidth, 18);
            gfx.DrawString("ข้อมูลส่วน HRD บันทึกข้อมูล", _fontBold, XBrushes.Black, new XPoint(rightX + 5, yRight + 13));

            yRight += 18;

            gfx.DrawRectangle(_borderPen, rightX, yRight, leftWidth, leftContentHeight);

            yRight += 12;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("เพิ่มในแผนฝึกอบรม ประจำปี:", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));
            gfx.DrawLine(_thinPen, rightX + 165, yRight + 2, rightX + leftWidth - 5, yRight + 2);

            yRight += 12;
            gfx.DrawString("ประเภท:", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));
            DrawCheckbox(gfx, rightX + 55, yRight - 8, false);
            gfx.DrawString("Technical", _fontSmall, XBrushes.Black, new XPoint(rightX + 70, yRight));
            DrawCheckbox(gfx, rightX + 130, yRight - 8, false);
            gfx.DrawString("Non-Technical", _fontSmall, XBrushes.Black, new XPoint(rightX + 145, yRight));

            yRight += 12;
            gfx.DrawString("สังกัดงบประมาณ:", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));
            gfx.DrawLine(_thinPen, rightX + 95, yRight + 2, rightX + leftWidth - 5, yRight + 2);

            yRight += 12;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("จัดเก็บเอกสาร", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));

            yRight += 12;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("บันทึกข้อมูลในระบบ", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));

            yRight += 12;
            gfx.DrawString("ผู้บันทึก:", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));
            gfx.DrawLine(_thinPen, rightX + 60, yRight + 2, rightX + leftWidth - 5, yRight + 2);

            yRight += 12;
            gfx.DrawString("วันที่:", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));
            gfx.DrawLine(_thinPen, rightX + 45, yRight + 2, rightX + leftWidth - 5, yRight + 2);

            yPos = startY + 18 + leftContentHeight + 10;
        }

        private void DrawApprovers(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("ผู้อนุมัติ (Approvers):", _fontHeader, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 20;

            string[] approverTitles = {
                "1. Section Manager (หัวหน้าแผนก)",
                "2. Department Manager (ผู้จัดการฝ่าย)",
                "3. HRD Admin (ฝ่ายทรัพยากรบุคคล - ตรวจสอบ)",
                "4. HRD Confirmation (ฝ่ายทรัพยากรบุคคล - อนุมัติ)",
                "5. Managing Director (กรรมการผู้จัดการ)"
            };

            string[] approverIds = {
                data.SectionManagerId,
                data.DepartmentManagerId,
                data.HRDAdminId,
                data.HRDConfirmationId,
                data.ManagingDirectorId
            };

            string[] statuses = {
                data.Status_SectionManager,
                data.Status_DepartmentManager,
                data.Status_HRDAdmin,
                data.Status_HRDConfirmation,
                data.Status_ManagingDirector
            };

            string[] approveInfos = {
                data.ApproveInfo_SectionManager,
                data.ApproveInfo_DepartmentManager,
                data.ApproveInfo_HRDAdmin,
                data.ApproveInfo_HRDConfirmation,
                data.ApproveInfo_ManagingDirector
            };

            for (int i = 0; i < approverTitles.Length; i++)
            {
                gfx.DrawString(approverTitles[i], _fontBold, XBrushes.Black, new XPoint(x + 10, yPos));
                yPos += 12;

                // แสดงเฉพาะถ้า Status = APPROVED
                if (!string.IsNullOrEmpty(statuses[i]) && statuses[i].ToUpper() == "APPROVED")
                {
                    gfx.DrawString($"   ผู้อนุมัติ: {approverIds[i] ?? "-"}", _fontSmall, XBrushes.Black, new XPoint(x + 30, yPos));
                    yPos += 12;

                    gfx.DrawString($"   วันที่อนุมัติ: {approveInfos[i] ?? "-"}", _fontSmall, XBrushes.Black, new XPoint(x + 30, yPos));
                    yPos += 12;
                }
                else
                {
                    gfx.DrawString($"   สถานะ: {statuses[i] ?? "รออนุมัติ"}", _fontSmall, XBrushes.Gray, new XPoint(x + 30, yPos));
                    yPos += 12;
                }

                yPos += 5;
            }

            // Footer info
            yPos += 10;
            gfx.DrawString($"สถานะเอกสาร: {data.Status ?? "Draft"}", _fontSmall, XBrushes.Black, new XPoint(x, yPos));
            gfx.DrawString($"วันที่สร้าง: {data.CreatedDate:dd/MM/yyyy HH:mm}", _fontSmall, XBrushes.Black, new XPoint(x + 200, yPos));
        }

        private void DrawCheckbox(XGraphics gfx, double x, double y, bool isChecked)
        {
            double size = 10;
            gfx.DrawRectangle(_borderPen, x, y, size, size);
            if (isChecked)
            {
                // Draw X
                gfx.DrawLine(_borderPen, x + 2, y + 2, x + size - 2, y + size - 2);
                gfx.DrawLine(_borderPen, x + 2, y + size - 2, x + size - 2, y + 2);
            }
        }

        // ==========================================
        // DATABASE METHODS
        // ==========================================

        private async Task<TrainingRequestData> GetTrainingRequestDataAsync(int id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

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
                            return new TrainingRequestData
                            {
                                DocNo = reader["DocNo"]?.ToString(),
                                Company = reader["Company"]?.ToString(),
                                TrainingType = reader["TrainingType"]?.ToString(),
                                Factory = reader["Factory"]?.ToString(),
                                Department = reader["Department"]?.ToString(),
                                Position = reader["Position"]?.ToString(),
                                SeminarTitle = reader["SeminarTitle"]?.ToString(),
                                TrainingLocation = reader["TrainingLocation"]?.ToString(),
                                Instructor = reader["Instructor"]?.ToString(),
                                StartDate = reader["StartDate"] != DBNull.Value ? (DateTime?)reader["StartDate"] : null,
                                EndDate = reader["EndDate"] != DBNull.Value ? (DateTime?)reader["EndDate"] : null,
                                TotalPeople = reader["TotalPeople"] != DBNull.Value ? (int)reader["TotalPeople"] : 0,
                                PerPersonTrainingHours = reader["PerPersonTrainingHours"] != DBNull.Value ? (int)reader["PerPersonTrainingHours"] : 0,
                                RegistrationCost = reader["RegistrationCost"] != DBNull.Value ? (decimal)reader["RegistrationCost"] : 0,
                                InstructorFee = reader["InstructorFee"] != DBNull.Value ? (decimal)reader["InstructorFee"] : 0,
                                EquipmentCost = reader["EquipmentCost"] != DBNull.Value ? (decimal)reader["EquipmentCost"] : 0,
                                FoodCost = reader["FoodCost"] != DBNull.Value ? (decimal)reader["FoodCost"] : 0,
                                OtherCost = reader["OtherCost"] != DBNull.Value ? (decimal)reader["OtherCost"] : 0,
                                TotalCost = reader["TotalCost"] != DBNull.Value ? (decimal)reader["TotalCost"] : 0,
                                CostPerPerson = reader["CostPerPerson"] != DBNull.Value ? (decimal)reader["CostPerPerson"] : 0,
                                TrainingObjective = reader["TrainingObjective"]?.ToString(),
                                OtherObjective = reader["OtherObjective"]?.ToString(),
                                ExpectedOutcome = reader["ExpectedOutcome"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime)reader["CreatedDate"] : DateTime.Now,
                                SectionManagerId = reader["SectionManagerId"]?.ToString(),
                                Status_SectionManager = reader["Status_SectionManager"]?.ToString(),
                                ApproveInfo_SectionManager = reader["ApproveInfo_SectionManager"]?.ToString(),
                                DepartmentManagerId = reader["DepartmentManagerId"]?.ToString(),
                                Status_DepartmentManager = reader["Status_DepartmentManager"]?.ToString(),
                                ApproveInfo_DepartmentManager = reader["ApproveInfo_DepartmentManager"]?.ToString(),
                                HRDAdminId = reader["HRDAdminId"]?.ToString(),
                                Status_HRDAdmin = reader["Status_HRDAdmin"]?.ToString(),
                                ApproveInfo_HRDAdmin = reader["ApproveInfo_HRDAdmin"]?.ToString(),
                                HRDConfirmationId = reader["HRDConfirmationId"]?.ToString(),
                                Status_HRDConfirmation = reader["Status_HRDConfirmation"]?.ToString(),
                                ApproveInfo_HRDConfirmation = reader["ApproveInfo_HRDConfirmation"]?.ToString(),
                                ManagingDirectorId = reader["ManagingDirectorId"]?.ToString(),
                                Status_ManagingDirector = reader["Status_ManagingDirector"]?.ToString(),
                                ApproveInfo_ManagingDirector = reader["ApproveInfo_ManagingDirector"]?.ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        // ==========================================
        // DATA CLASS
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
        }
    }
}
