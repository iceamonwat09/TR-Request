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
        /// <param name="docNo">เลขที่เอกสาร (สำหรับ Log)</param>
        /// <returns>true ถ้าส่งสำเร็จ, false ถ้าล้มเหลว</returns>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null, string? docNo = null);

        /// <summary>
        /// ส่ง Email ให้หลายคน
        /// </summary>
        Task<bool> SendEmailToMultipleAsync(string[] toEmails, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null, string? docNo = null);

        /// <summary>
        /// ส่ง Email แบบมี CC (To + CC ในฉบับเดียว)
        /// </summary>
        /// <param name="toEmail">Email ผู้รับหลัก</param>
        /// <param name="ccEmails">รายการ Email สำหรับ CC (optional)</param>
        /// <param name="subject">หัวข้อ Email</param>
        /// <param name="htmlBody">เนื้อหา HTML</param>
        /// <param name="trainingRequestId">ID ของ TrainingRequest (สำหรับ Log)</param>
        /// <param name="emailType">ประเภทของ Email</param>
        /// <param name="docNo">เลขที่เอกสาร (สำหรับ Log)</param>
        /// <returns>true ถ้าส่งสำเร็จ, false ถ้าล้มเหลว</returns>
        Task<bool> SendEmailWithCCAsync(string toEmail, string[]? ccEmails, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null, string? docNo = null);

        /// <summary>
        /// ส่ง Email ให้หลายคนในฉบับเดียว (To หลายคน)
        /// </summary>
        /// <param name="toEmails">รายการ Email ผู้รับ (ทุกคนเห็นกัน)</param>
        /// <param name="subject">หัวข้อ Email</param>
        /// <param name="htmlBody">เนื้อหา HTML</param>
        /// <param name="trainingRequestId">ID ของ TrainingRequest (สำหรับ Log)</param>
        /// <param name="emailType">ประเภทของ Email</param>
        /// <param name="docNo">เลขที่เอกสาร (สำหรับ Log)</param>
        /// <returns>true ถ้าส่งสำเร็จ, false ถ้าล้มเหลว</returns>
        Task<bool> SendEmailToMultipleRecipientsAsync(string[] toEmails, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null, string? docNo = null);
    }
}
