using System.Threading.Tasks;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// Interface for PDF Report Generation Service
    /// สร้างรายงาน PDF ตามแบบฟอร์มคำขอฝึกอบรม
    /// </summary>
    public interface IPdfReportService
    {
        /// <summary>
        /// Generate PDF report for a training request
        /// </summary>
        /// <param name="trainingRequestId">Training Request ID</param>
        /// <returns>PDF file as byte array</returns>
        Task<byte[]> GenerateTrainingRequestPdfAsync(int trainingRequestId);
    }
}
