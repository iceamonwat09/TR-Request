# PowerShell script to test SMTP connection
# รันใน PowerShell (Windows)

$smtpServer = "smtp.gmail.com"
$smtpPort = 587
$username = "HRDTrainingRequest@gmail.com"
$password = "refnhjslcthakpsi"  # App Password

try {
    $smtpClient = New-Object Net.Mail.SmtpClient($smtpServer, $smtpPort)
    $smtpClient.EnableSsl = $true
    $smtpClient.Credentials = New-Object Net.NetworkCredential($username, $password)

    $mailMessage = New-Object Net.Mail.MailMessage
    $mailMessage.From = $username
    $mailMessage.To.Add($username)  # ส่งให้ตัวเอง
    $mailMessage.Subject = "Test Email from PowerShell"
    $mailMessage.Body = "This is a test email"

    $smtpClient.Send($mailMessage)
    Write-Host "✅ Email sent successfully!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
