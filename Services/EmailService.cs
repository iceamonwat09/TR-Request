using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TrainingRequestApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                Console.WriteLine("⚠️ Email recipient is empty");
                return false;
            }

            try
            {
                // ดึงค่า Email Settings จาก appsettings.json
                var emailSettings = _configuration.GetSection("EmailSettings");

                string smtpServer = emailSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                string smtpUsername = emailSettings["SmtpUsername"] ?? "";
                string smtpPassword = emailSettings["SmtpPassword"] ?? "";
                string fromEmail = emailSettings["FromEmail"] ?? "noreply@company.com";
                string fromName = emailSettings["FromName"] ?? "Training Request System";
                bool enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                // ⭐ Debug logging
                Console.WriteLine("📧 Email Configuration:");
                Console.WriteLine($"   SMTP Server: {smtpServer}:{smtpPort}");
                Console.WriteLine($"   Username: {smtpUsername}");
                Console.WriteLine($"   From: {fromEmail}");
                Console.WriteLine($"   SSL: {enableSsl}");
                Console.WriteLine($"   To: {toEmail}");

                // สร้าง SMTP Client
                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = enableSsl;

                    // สร้าง Mail Message
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    Console.WriteLine("📤 Attempting to send email...");

                    // ส่ง Email
                    await smtpClient.SendMailAsync(mailMessage);

                    Console.WriteLine($"✅ Email sent successfully to: {toEmail}");
                    Console.WriteLine($"   Subject: {subject}");

                    // บันทึก Log
                    await LogEmail(trainingRequestId, null, toEmail, emailType ?? "UNKNOWN", subject, "SENT", null);

                    return true;
                }
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"❌ SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"   StatusCode: {smtpEx.StatusCode}");
                Console.WriteLine($"   To: {toEmail}");
                Console.WriteLine($"   Subject: {subject}");
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {smtpEx.InnerException.Message}");
                }

                // บันทึก Log ที่ล้มเหลว
                await LogEmail(trainingRequestId, null, toEmail, emailType ?? "UNKNOWN", subject, "FAILED",
                    $"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email Error: {ex.Message}");
                Console.WriteLine($"   To: {toEmail}");
                Console.WriteLine($"   Subject: {subject}");

                // บันทึก Log ที่ล้มเหลว
                await LogEmail(trainingRequestId, null, toEmail, emailType ?? "UNKNOWN", subject, "FAILED", ex.Message);

                return false;
            }
        }

        public async Task<bool> SendEmailToMultipleAsync(string[] toEmails, string subject, string htmlBody, int? trainingRequestId = null, string? emailType = null)
        {
            bool allSuccess = true;

            foreach (var email in toEmails)
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    bool success = await SendEmailAsync(email.Trim(), subject, htmlBody, trainingRequestId, emailType);
                    if (!success)
                        allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// บันทึก Email Log ลง Database
        /// </summary>
        private async Task LogEmail(int? trainingRequestId, string? docNo, string recipientEmail, string emailType, string subject, string status, string? errorMessage)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        INSERT INTO [HRDSYSTEM].[dbo].[EmailLogs]
                        ([TrainingRequestId], [DocNo], [RecipientEmail], [EmailType], [Subject], [SentDate], [Status], [ErrorMessage])
                        VALUES
                        (@TrainingRequestId, @DocNo, @RecipientEmail, @EmailType, @Subject, GETDATE(), @Status, @ErrorMessage)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocNo", docNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RecipientEmail", recipientEmail);
                        cmd.Parameters.AddWithValue("@EmailType", emailType);
                        cmd.Parameters.AddWithValue("@Subject", subject);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to log email: {ex.Message}");
                // ไม่ throw exception เพราะไม่ต้องการให้ Log ที่ล้มเหลวทำให้การส่ง Email หลักล้มเหลว
            }
        }
    }
}
