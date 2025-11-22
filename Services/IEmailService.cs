using System.Threading.Tasks;

namespace TrainingRequestApp.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// ส่ง Email แบบ HTML
        /// </summary>
        /// <param name="toEmail">Email ผู้รับ</param>
        /// <param name="subject">หัวข้อ Email</param>
        /// <param name="htmlBody">เนื้อหา HTML</param>
        /// <param name="trainingRequestId">ID ของ TrainingRequest (สำหรับ Log)</param>
        /// <param name="emailType">ประเภทของ Email</param>
        /// <returns>true ถ้าส่งสำเร็จ, false ถ้าล้มเหลว</returns>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null);

        /// <summary>
        /// ส่ง Email ให้หลายคน
        /// </summary>
        Task<bool> SendEmailToMultipleAsync(string[] toEmails, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null);
    }
}
