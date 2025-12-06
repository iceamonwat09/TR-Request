using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// PDF Report Generation Service using PdfSharpCore
    /// สร้างรายงานแบบฟอร์มคำขอฝึกอบรม (Training Request Form) เป็น PDF
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

        public PdfReportService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Initialize fonts with Unicode support for Thai language
            // ใช้ XPdfFontOptions.UnicodeDefault เพื่อรองรับภาษาไทย
            var options = new XPdfFontOptions(PdfFontEncoding.Unicode);

            // ใช้ Tahoma หรือ Microsoft Sans Serif ซึ่งรองรับภาษาไทยดีกว่า Arial
            string fontName = "Tahoma"; // หรือ "Microsoft Sans Serif"

            _fontTitle = new XFont(fontName, 16, XFontStyle.Bold, options);
            _fontHeader = new XFont(fontName, 12, XFontStyle.Bold, options);
            _fontNormal = new XFont(fontName, 10, XFontStyle.Regular, options);
            _fontSmall = new XFont(fontName, 8, XFontStyle.Regular, options);
            _fontBold = new XFont(fontName, 10, XFontStyle.Bold, options);
        }

        /// <summary>
        /// Generate PDF report for a training request
        /// </summary>
        public async Task<byte[]> GenerateTrainingRequestPdfAsync(int trainingRequestId)
        {
            try
            {
                // 1. ดึงข้อมูลจาก Database
                var trainingData = await GetTrainingRequestDataAsync(trainingRequestId);

                if (trainingData == null)
                {
                    throw new Exception($"Training Request ID {trainingRequestId} not found");
                }

                // 2. สร้าง PDF Document
                PdfDocument document = new PdfDocument();
                document.Info.Title = $"Training Request - {trainingData.DocNo}";
                document.Info.Author = "TR-Request System";
                document.Info.Subject = "คำขอฝึกอบรม";
                document.Info.Creator = "PdfSharpCore";

                // 3. สร้างหน้า PDF (A4 size)
                PdfPage page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // 4. เริ่มวาด PDF
                double yPosition = 40; // เริ่มต้นที่ 40 pixels จากด้านบน
                double margin = 40;
                double pageWidth = page.Width - (2 * margin);

                // ===================================
                // HEADER - ชื่อฟอร์ม
                // ===================================
                DrawCenteredText(gfx, "แบบฟอร์มคำขอฝึกอบรม", _fontTitle, ref yPosition, page.Width);
                yPosition += 10;
                DrawCenteredText(gfx, "TRAINING REQUEST FORM", _fontNormal, ref yPosition, page.Width);
                yPosition += 20;

                // ===================================
                // SECTION 1: ข้อมูลเอกสาร
                // ===================================
                DrawSectionHeader(gfx, "ข้อมูลเอกสาร", margin, ref yPosition, pageWidth);
                yPosition += 5;

                DrawLabelValue(gfx, "เลขที่เอกสาร:", trainingData.DocNo ?? "-", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "บริษัท:", trainingData.Company ?? "-", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ประเภทการอบรม:", trainingData.TrainingType ?? "-", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "แผนก:", trainingData.Department ?? "-", margin, ref yPosition, pageWidth);
                yPosition += 10;

                // ===================================
                // SECTION 2: ข้อมูลหลักสูตร
                // ===================================
                DrawSectionHeader(gfx, "ข้อมูลหลักสูตร", margin, ref yPosition, pageWidth);
                yPosition += 5;

                DrawLabelValue(gfx, "ชื่อหลักสูตร:", trainingData.SeminarTitle ?? "-", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "สถานที่อบรม:", trainingData.TrainingLocation ?? "-", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "วิทยากร/สถาบัน:", trainingData.Instructor ?? "-", margin, ref yPosition, pageWidth);

                string dateRange = "";
                if (trainingData.StartDate.HasValue && trainingData.EndDate.HasValue)
                {
                    dateRange = $"{trainingData.StartDate.Value:dd/MM/yyyy} - {trainingData.EndDate.Value:dd/MM/yyyy}";
                }
                DrawLabelValue(gfx, "วันที่อบรม:", dateRange, margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "จำนวนผู้เข้าอบรม:", $"{trainingData.TotalPeople} คน", margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ชั่วโมงต่อคน:", $"{trainingData.PerPersonTrainingHours} ชม.", margin, ref yPosition, pageWidth);
                yPosition += 10;

                // ===================================
                // SECTION 3: งบประมาณ
                // ===================================
                DrawSectionHeader(gfx, "รายละเอียดงบประมาณ", margin, ref yPosition, pageWidth);
                yPosition += 5;

                DrawLabelValue(gfx, "ค่าลงทะเบียน:", FormatCurrency(trainingData.RegistrationCost), margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ค่าวิทยากร:", FormatCurrency(trainingData.InstructorFee), margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ค่าอุปกรณ์:", FormatCurrency(trainingData.EquipmentCost), margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ค่าอาหาร:", FormatCurrency(trainingData.FoodCost), margin, ref yPosition, pageWidth);
                DrawLabelValue(gfx, "ค่าใช้จ่ายอื่นๆ:", FormatCurrency(trainingData.OtherCost), margin, ref yPosition, pageWidth);

                // Draw line
                gfx.DrawLine(XPens.Black, margin + 20, yPosition, margin + pageWidth - 20, yPosition);
                yPosition += 5;

                DrawLabelValue(gfx, "รวมทั้งหมด:", FormatCurrency(trainingData.TotalCost), margin, ref yPosition, pageWidth, _fontBold);
                DrawLabelValue(gfx, "ค่าใช้จ่ายต่อคน:", FormatCurrency(trainingData.CostPerPerson), margin, ref yPosition, pageWidth);
                yPosition += 10;

                // ===================================
                // SECTION 4: วัตถุประสงค์
                // ===================================
                DrawSectionHeader(gfx, "วัตถุประสงค์", margin, ref yPosition, pageWidth);
                yPosition += 5;
                DrawMultilineText(gfx, trainingData.TrainingObjective ?? "-", margin + 20, ref yPosition, pageWidth - 20);

                if (!string.IsNullOrEmpty(trainingData.OtherObjective))
                {
                    yPosition += 5;
                    DrawLabel(gfx, "วัตถุประสงค์อื่นๆ:", margin, ref yPosition);
                    DrawMultilineText(gfx, trainingData.OtherObjective, margin + 20, ref yPosition, pageWidth - 20);
                }
                yPosition += 10;

                // ===================================
                // SECTION 5: ผลที่คาดหวัง
                // ===================================
                if (!string.IsNullOrEmpty(trainingData.ExpectedOutcome))
                {
                    DrawSectionHeader(gfx, "ผลที่คาดหวัง", margin, ref yPosition, pageWidth);
                    yPosition += 5;
                    DrawMultilineText(gfx, trainingData.ExpectedOutcome, margin + 20, ref yPosition, pageWidth - 20);
                    yPosition += 10;
                }

                // Check if need new page
                if (yPosition > page.Height - 200)
                {
                    page = document.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;
                    gfx = XGraphics.FromPdfPage(page);
                    yPosition = 40;
                }

                // ===================================
                // SECTION 6: ผู้อนุมัติ (แสดงเฉพาะที่ APPROVED)
                // ===================================
                DrawSectionHeader(gfx, "ผู้อนุมัติ", margin, ref yPosition, pageWidth);
                yPosition += 10;

                // Section Manager
                DrawApproverSection(gfx, "1. Section Manager",
                    trainingData.SectionManagerId,
                    trainingData.Status_SectionManager,
                    trainingData.ApproveInfo_SectionManager,
                    margin, ref yPosition, pageWidth);

                // Department Manager
                DrawApproverSection(gfx, "2. Department Manager",
                    trainingData.DepartmentManagerId,
                    trainingData.Status_DepartmentManager,
                    trainingData.ApproveInfo_DepartmentManager,
                    margin, ref yPosition, pageWidth);

                // HRD Admin
                DrawApproverSection(gfx, "3. HRD Admin",
                    trainingData.HRDAdminId,
                    trainingData.Status_HRDAdmin,
                    trainingData.ApproveInfo_HRDAdmin,
                    margin, ref yPosition, pageWidth);

                // HRD Confirmation
                DrawApproverSection(gfx, "4. HRD Confirmation",
                    trainingData.HRDConfirmationId,
                    trainingData.Status_HRDConfirmation,
                    trainingData.ApproveInfo_HRDConfirmation,
                    margin, ref yPosition, pageWidth);

                // Managing Director
                DrawApproverSection(gfx, "5. Managing Director",
                    trainingData.ManagingDirectorId,
                    trainingData.Status_ManagingDirector,
                    trainingData.ApproveInfo_ManagingDirector,
                    margin, ref yPosition, pageWidth);

                // ===================================
                // FOOTER
                // ===================================
                yPosition = page.Height - 60;
                gfx.DrawLine(XPens.Gray, margin, yPosition, page.Width - margin, yPosition);
                yPosition += 5;
                DrawCenteredText(gfx, $"เอกสารสร้างโดยระบบ TR-Request | {DateTime.Now:dd/MM/yyyy HH:mm}", _fontSmall, ref yPosition, page.Width);
                DrawCenteredText(gfx, $"Status: {trainingData.Status}", _fontSmall, ref yPosition, page.Width);

                // 5. Save to Memory Stream
                using (var stream = new System.IO.MemoryStream())
                {
                    document.Save(stream, false);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating PDF: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // ===================================
        // HELPER METHODS
        // ===================================

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

        private void DrawCenteredText(XGraphics gfx, string text, XFont font, ref double yPos, double pageWidth)
        {
            XSize textSize = gfx.MeasureString(text, font);
            double xPos = (pageWidth - textSize.Width) / 2;
            gfx.DrawString(text, font, XBrushes.Black, new XPoint(xPos, yPos));
            yPos += textSize.Height + 5;
        }

        private void DrawSectionHeader(XGraphics gfx, string text, double x, ref double yPos, double width)
        {
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(230, 230, 230)), x, yPos - 15, width, 20);
            gfx.DrawString(text, _fontHeader, XBrushes.Black, new XPoint(x + 5, yPos));
            yPos += 10;
        }

        private void DrawLabelValue(XGraphics gfx, string label, string value, double x, ref double yPos, double width, XFont font = null)
        {
            font = font ?? _fontNormal;
            XFont labelFont = new XFont(font.Name, font.Size, XFontStyle.Bold);

            gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(x + 20, yPos));
            gfx.DrawString(value ?? "-", font, XBrushes.Black, new XPoint(x + 150, yPos));
            yPos += 15;
        }

        private void DrawLabel(XGraphics gfx, string label, double x, ref double yPos)
        {
            gfx.DrawString(label, _fontBold, XBrushes.Black, new XPoint(x + 20, yPos));
            yPos += 15;
        }

        private void DrawMultilineText(XGraphics gfx, string text, double x, ref double yPos, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) text = "-";

            // Simple word wrap
            string[] words = text.Split(' ');
            string currentLine = "";

            foreach (string word in words)
            {
                string testLine = currentLine + word + " ";
                XSize size = gfx.MeasureString(testLine, _fontNormal);

                if (size.Width > maxWidth && currentLine.Length > 0)
                {
                    gfx.DrawString(currentLine.Trim(), _fontNormal, XBrushes.Black, new XPoint(x, yPos));
                    yPos += 15;
                    currentLine = word + " ";
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (currentLine.Length > 0)
            {
                gfx.DrawString(currentLine.Trim(), _fontNormal, XBrushes.Black, new XPoint(x, yPos));
                yPos += 15;
            }
        }

        private void DrawApproverSection(XGraphics gfx, string title, string approverId, string status, string approveInfo, double x, ref double yPos, double width)
        {
            gfx.DrawString(title, _fontBold, XBrushes.Black, new XPoint(x + 20, yPos));
            yPos += 15;

            // เงื่อนไข: แสดงชื่อและเวลา เฉพาะถ้า Status = "APPROVED"
            if (!string.IsNullOrEmpty(status) && status.ToUpper() == "APPROVED")
            {
                gfx.DrawString($"   ผู้อนุมัติ: {approverId ?? "-"}", _fontNormal, XBrushes.Black, new XPoint(x + 40, yPos));
                yPos += 15;

                gfx.DrawString($"   วันที่อนุมัติ: {approveInfo ?? "-"}", _fontNormal, XBrushes.Black, new XPoint(x + 40, yPos));
                yPos += 15;
            }
            else
            {
                gfx.DrawString($"   สถานะ: {status ?? "รออนุมัติ"}", _fontSmall, XBrushes.Gray, new XPoint(x + 40, yPos));
                yPos += 15;
            }

            yPos += 5;
        }

        private string FormatCurrency(decimal? amount)
        {
            if (!amount.HasValue) return "0.00 บาท";
            return $"{amount.Value:N2} บาท";
        }

        // ===================================
        // DATA MODEL
        // ===================================

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

            // Approvers
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
