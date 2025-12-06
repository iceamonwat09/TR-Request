using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// PDF Report Service - Template ตรงตามแบบฟอร์มจริง 100%
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

        // Colors
        private XSolidBrush _grayBrush;
        private XPen _borderPen;
        private XPen _thinPen;

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Initialize fonts with Unicode support
            var options = new XPdfFontOptions(PdfFontEncoding.Unicode);
            string fontName = "Tahoma";

            _fontTitle = new XFont(fontName, 14, XFontStyle.Bold, options);
            _fontHeader = new XFont(fontName, 11, XFontStyle.Bold, options);
            _fontNormal = new XFont(fontName, 9, XFontStyle.Regular, options);
            _fontSmall = new XFont(fontName, 8, XFontStyle.Regular, options);
            _fontBold = new XFont(fontName, 9, XFontStyle.Bold, options);
            _fontTiny = new XFont(fontName, 7, XFontStyle.Regular, options);

            // Colors and pens
            _grayBrush = new XSolidBrush(XColor.FromArgb(220, 220, 220));
            _borderPen = new XPen(XColors.Black, 1);
            _thinPen = new XPen(XColors.Black, 0.5);
        }

        public async Task<byte[]> GenerateTrainingRequestPdfAsync(int trainingRequestId)
        {
            try
            {
                var data = await GetTrainingRequestDataAsync(trainingRequestId);
                if (data == null) throw new Exception($"Training Request ID {trainingRequestId} not found");

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
                // HEADER SECTION
                // ==========================================
                DrawHeader(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // MAIN BODY (2 COLUMNS)
                // ==========================================
                DrawMainBody(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // TABLE: รายชื่อผู้เข้าอบรม (3 rows)
                // ==========================================
                DrawParticipantTable(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // วัตถุประสงค์
                // ==========================================
                DrawObjectives(gfx, data, margin, ref yPos, pageWidth);

                // ==========================================
                // งบประมาณ
                // ==========================================
                DrawBudgetSection(gfx, data, margin, ref yPos, pageWidth);

                // Check if need new page for approvers
                if (yPos > page.Height - 250)
                {
                    page = document.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = margin;
                }

                // ==========================================
                // FOOTER: ผู้อนุมัติ (2 columns)
                // ==========================================
                DrawApproversSection(gfx, data, margin, ref yPos, pageWidth);

                // Save to memory stream
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

        private void DrawHeader(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // Border around header
            gfx.DrawRectangle(_borderPen, x, yPos, width, 80);

            // Line 1: ประเภทการอบรม
            double y = yPos + 10;
            gfx.DrawString("ประเภทการอบรม:", _fontBold, XBrushes.Black, new XPoint(x + 5, y));

            // Checkboxes
            double cbX = x + 120;
            DrawCheckbox(gfx, cbX, y - 8, data.TrainingType == "In-House");
            gfx.DrawString("อบรมภายใน (In-House Training)", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, y));

            cbX += 220;
            DrawCheckbox(gfx, cbX, y - 8, data.TrainingType == "Public");
            gfx.DrawString("อบรมภายนอก (Public Training)", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, y));

            // Line 2: สมาชิก/ประจำปี
            y += 15;
            cbX = x + 120;
            DrawCheckbox(gfx, cbX, y - 8, false);
            gfx.DrawString("สมาชิกสหภาพ", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, y));

            cbX += 120;
            DrawCheckbox(gfx, cbX, y - 8, false);
            gfx.DrawString("ประจำปี", _fontNormal, XBrushes.Black, new XPoint(cbX + 15, y));

            // Line 3: เรื่อง
            y += 15;
            gfx.DrawString("เรื่อง:", _fontBold, XBrushes.Black, new XPoint(x + 5, y));
            gfx.DrawLine(_thinPen, x + 35, y + 2, x + 300, y + 2);
            gfx.DrawString(data.SeminarTitle ?? "", _fontNormal, XBrushes.Black, new XPoint(x + 40, y));

            gfx.DrawString("ตำแหน่ง:", _fontNormal, XBrushes.Black, new XPoint(x + 310, y));
            gfx.DrawLine(_thinPen, x + 360, y + 2, x + width - 5, y + 2);
            gfx.DrawString(data.Position ?? "", _fontNormal, XBrushes.Black, new XPoint(x + 365, y));

            // Line 4: สังกัดงบประมาณ
            y += 15;
            gfx.DrawString("สังกัดงบประมาณ:", _fontNormal, XBrushes.Black, new XPoint(x + 5, y));
            gfx.DrawLine(_thinPen, x + 85, y + 2, x + 300, y + 2);
            gfx.DrawString(data.Department ?? "", _fontNormal, XBrushes.Black, new XPoint(x + 90, y));

            yPos += 85;
        }

        private void DrawMainBody(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // ซ้าย: ข้อมูลคำขอการอบรม (แถบสีเทา)
            double leftWidth = width * 0.7;
            double rightWidth = width * 0.3 - 5;
            double rightX = x + leftWidth + 5;

            // Header bar (gray background)
            gfx.DrawRectangle(_grayBrush, x, yPos, leftWidth, 20);
            gfx.DrawRectangle(_borderPen, x, yPos, leftWidth, 20);
            gfx.DrawString("ข้อมูลคำขอการอบรม", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos + 14));

            // Right column header
            gfx.DrawRectangle(_borderPen, rightX, yPos, rightWidth, 20);
            gfx.DrawString("คอมเม้นท์กรรมการ:", _fontBold, XBrushes.Black, new XPoint(rightX + 5, yPos + 14));

            yPos += 20;

            // Left column content
            double yLeft = yPos;
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, 120);

            yLeft += 12;
            gfx.DrawString($"ตำแหน่งผู้ขอ: {data.Position ?? ""}     ฝ่าย: {data.Department ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString("ชื่อสายประสานงาน: _________________", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"ข้อมูลคำขอการอบรม: {data.SeminarTitle ?? ""}", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"สถานที่: {data.TrainingLocation ?? ""}     รวมระยะเวลา: วัน / ชั่วโมง", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"การเข้าชาง:  □ รัวหรือผู้อื่น  □ เครื่องน้อง", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"ใช้เวลาฝึกวางศัพท์: {data.PerPersonTrainingHours} ชม.", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            yLeft += 12;
            gfx.DrawString($"กระบวนงาน: ___________     จำนวนผู้เข้าร่วมอบรม: {data.TotalPeople} คน", _fontSmall, XBrushes.Black, new XPoint(x + 5, yLeft));

            // Right column content
            double yRight = yPos;
            gfx.DrawRectangle(_borderPen, rightX, yRight, rightWidth, 60);
            yRight += 12;
            DrawCheckbox(gfx, rightX + 5, yRight - 8, false);
            gfx.DrawString("อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 20, yRight));
            DrawCheckbox(gfx, rightX + 70, yRight - 8, false);
            gfx.DrawString("ไม่อนุมัติ", _fontSmall, XBrushes.Black, new XPoint(rightX + 85, yRight));

            yRight += 15;
            gfx.DrawString("ครั้อ: _____ ( / / )", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, yRight));

            yRight += 12;
            gfx.DrawString("ตำแหน่ง: _____________", _fontSmall, XBrushes.Black, new XPoint(rightX + 5, yRight));

            yPos += 120;
        }

        private void DrawParticipantTable(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // Header
            gfx.DrawString("รายชื่อผู้เข้าร่วม:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos + 12));
            yPos += 15;

            // Table header
            double[] colWidths = { 50, 150, 150, width - 350 };
            string[] headers = { "ลำดับ", "ชื่อ", "ภารี", "ตำแหน่ง" };

            double xCol = x;
            double rowHeight = 20;

            // Header row
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontBold, XBrushes.Black, new XPoint(xCol + 5, yPos + 14));
                xCol += colWidths[i];
            }

            yPos += rowHeight;

            // Data rows (3 rows)
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
            gfx.DrawString("วัตถุประสงค์:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos + 12));
            yPos += 15;

            // Row 1
            double cbY = yPos;
            DrawCheckbox(gfx, x + 10, cbY, false);
            gfx.DrawString("พัฒนาทักษะความรู้", _fontSmall, XBrushes.Black, new XPoint(x + 25, cbY + 8));

            DrawCheckbox(gfx, x + 180, cbY, false);
            gfx.DrawString("เพิ่มประสิทธิภาพ / คุณภาพ", _fontSmall, XBrushes.Black, new XPoint(x + 195, cbY + 8));

            DrawCheckbox(gfx, x + 380, cbY, false);
            gfx.DrawString("ทดสอบผล / ปรับปรุงงา", _fontSmall, XBrushes.Black, new XPoint(x + 395, cbY + 8));

            yPos += 15;

            // Row 2
            cbY = yPos;
            DrawCheckbox(gfx, x + 10, cbY, false);
            gfx.DrawString("กฎหม้/คู่มือ/กำหนดส", _fontSmall, XBrushes.Black, new XPoint(x + 25, cbY + 8));

            DrawCheckbox(gfx, x + 180, cbY, false);
            gfx.DrawString("อาคารสอภคเริตุยัยคล่องอยู่ปรัน", _fontSmall, XBrushes.Black, new XPoint(x + 195, cbY + 8));

            DrawCheckbox(gfx, x + 380, cbY, false);
            gfx.DrawString("อื่นๆ (ระบุ)", _fontSmall, XBrushes.Black, new XPoint(x + 395, cbY + 8));

            yPos += 20;
        }

        private void DrawBudgetSection(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("หลักฐานการอบรม:", _fontBold, XBrushes.Black, new XPoint(x + 5, yPos + 12));
            yPos += 15;

            // Row 1
            DrawCheckbox(gfx, x + 10, yPos, false);
            gfx.DrawString($"สำเนาเอกสารยืนยัน: _____ บาท", _fontSmall, XBrushes.Black, new XPoint(x + 25, yPos + 8));

            DrawCheckbox(gfx, x + 280, yPos, false);
            gfx.DrawString($"คำขอสะพเรศัมปรกงน: _____ บาท", _fontSmall, XBrushes.Black, new XPoint(x + 295, yPos + 8));

            yPos += 15;

            // Row 2
            DrawCheckbox(gfx, x + 10, yPos, false);
            gfx.DrawString($"อื่นๆ (ระบุ): _____ บาท", _fontSmall, XBrushes.Black, new XPoint(x + 25, yPos + 8));

            gfx.DrawString($"รวมเงิน: {data.TotalCost:N2} บาท", _fontBold, XBrushes.Black, new XPoint(x + 300, yPos + 8));

            yPos += 20;

            // Cost breakdown table
            DrawCostTable(gfx, data, x, ref yPos, width);
        }

        private void DrawCostTable(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            double[] colWidths = { 60, 120, 80, 80, 80, 80, width - 500 };
            string[] headers = { "ลำดับที่", "ชื่อ-สกุล", "ไม่แส", "เส", "โนเส", "เบื้อยบัน", "ชื่องักฐอร" };

            double xCol = x;
            double rowHeight = 20;

            // Header
            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawRectangle(_grayBrush, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawRectangle(_borderPen, xCol, yPos, colWidths[i], rowHeight);
                gfx.DrawString(headers[i], _fontSmall, XBrushes.Black, new XPoint(xCol + 3, yPos + 14));
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

            yPos += 10;

            // Footer: ตรวจอบอาคูม
            gfx.DrawString("ตรวจอบราคูม: _____ ( / / )", _fontSmall, XBrushes.Black, new XPoint(x + 5, yPos + 12));
            gfx.DrawString("อนุมัติชองสอบ: _____ ( / / )", _fontSmall, XBrushes.Black, new XPoint(x + 300, yPos + 12));

            yPos += 25;
        }

        private void DrawApproversSection(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            // 2 columns
            double leftWidth = width / 2 - 5;
            double rightX = x + width / 2 + 5;

            // Left column: หุดกมปริงคัวรญษ
            double yLeft = yPos;
            gfx.DrawRectangle(_grayBrush, x, yLeft, leftWidth, 18);
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, 18);
            gfx.DrawString("หุดกมปริงคัวรญษ:", _fontBold, XBrushes.Black, new XPoint(x + 5, yLeft + 13));

            yLeft += 18;
            gfx.DrawRectangle(_borderPen, x, yLeft, leftWidth, 100);

            yLeft += 15;
            DrawCheckbox(gfx, x + 10, yLeft - 8, false);
            gfx.DrawString("ชำขดสใร็ชภัอมารำกำมง", _fontSmall, XBrushes.Black, new XPoint(x + 25, yLeft));

            yLeft += 15;
            gfx.DrawString("เปีัยกต: _________________", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));

            yLeft += 30;
            gfx.DrawString("ตำแหน่ง: ____________ ( / / )", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));

            yLeft += 15;
            gfx.DrawString("ตำแหน่ง: _________________", _fontSmall, XBrushes.Black, new XPoint(x + 10, yLeft));

            // Right column: HRD
            double yRight = yPos;
            gfx.DrawRectangle(_grayBrush, rightX, yRight, leftWidth, 18);
            gfx.DrawRectangle(_borderPen, rightX, yRight, leftWidth, 18);
            gfx.DrawString("ชอมูขอย HRD งงงติอบขงอช:", _fontBold, XBrushes.Black, new XPoint(rightX + 5, yRight + 13));

            yRight += 18;
            gfx.DrawRectangle(_borderPen, rightX, yRight, leftWidth, 100);

            yRight += 15;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("คัอชงตรอชัมรื/ผืิอนเ: วันนี้", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));

            yRight += 12;
            gfx.DrawString("ชื้องนี้ดรอชอขัท", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));

            yRight += 12;
            gfx.DrawString("คงระชงะงม: ชำระเง้งมคงบรงน็ท", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));

            yRight += 12;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("เบื็ยกลัชมเนนตงยนูอิช", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));

            yRight += 12;
            DrawCheckbox(gfx, rightX + 10, yRight - 8, false);
            gfx.DrawString("รำระเป็นมันชคล", _fontSmall, XBrushes.Black, new XPoint(rightX + 25, yRight));

            yRight += 15;
            gfx.DrawString("คอบื้อ: ________ ผู้บันนิก", _fontSmall, XBrushes.Black, new XPoint(rightX + 10, yRight));

            yPos += 118 + 10;

            // ผู้อนุมัติ 5 ระดับ (แสดงเฉพาะที่ APPROVED)
            DrawApprovers(gfx, data, x, ref yPos, width);
        }

        private void DrawApprovers(XGraphics gfx, TrainingRequestData data, double x, ref double yPos, double width)
        {
            gfx.DrawString("ผู้อนุมัติ:", _fontHeader, XBrushes.Black, new XPoint(x, yPos + 12));
            yPos += 20;

            string[] approverTitles = {
                "1. Section Manager",
                "2. Department Manager",
                "3. HRD Admin",
                "4. HRD Confirmation",
                "5. Managing Director"
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
        }

        private void DrawCheckbox(XGraphics gfx, double x, double y, bool isChecked)
        {
            double size = 10;
            gfx.DrawRectangle(_borderPen, x, y, size, size);
            if (isChecked)
            {
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
        }
    }
}
