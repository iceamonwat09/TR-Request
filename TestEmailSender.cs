using System;
using System.Net;
using System.Net.Mail;

namespace EmailTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🧪 Testing Gmail SMTP Connection...");
            Console.WriteLine("=====================================");

            try
            {
                string smtpServer = "smtp.gmail.com";
                int smtpPort = 587;
                string username = "HRDTrainingRequest@gmail.com";
                string password = "refnhjslcthakpsi";
                string fromEmail = "HRDTrainingRequest@gmail.com";
                string toEmail = "HRDTrainingRequest@gmail.com"; // ส่งให้ตัวเอง

                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                    smtpClient.EnableSsl = true;
                    smtpClient.Timeout = 30000; // 30 seconds

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "Training Request System"),
                        Subject = "Test Email - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Body = "<h1>Test Email</h1><p>This is a test email from Training Request System.</p>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    Console.WriteLine("📧 Sending email to: " + toEmail);
                    smtpClient.Send(mailMessage);

                    Console.WriteLine("✅ Email sent successfully!");
                    Console.WriteLine("=====================================");
                    Console.WriteLine("Check your Gmail inbox: " + toEmail);
                }
            }
            catch (SmtpException ex)
            {
                Console.WriteLine("❌ SMTP Error:");
                Console.WriteLine("   StatusCode: " + ex.StatusCode);
                Console.WriteLine("   Message: " + ex.Message);
                Console.WriteLine("   InnerException: " + ex.InnerException?.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ General Error:");
                Console.WriteLine("   Message: " + ex.Message);
                Console.WriteLine("   Type: " + ex.GetType().Name);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
