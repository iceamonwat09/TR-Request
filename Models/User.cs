using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    public int EmployeeId { get; set; }
    public string Account { get; set; }
    public string Email { get; set; }

    // ✅ เพิ่ม `Password` field (ใช้สำหรับรับค่าจาก Form เท่านั้น)
    [NotMapped] // ไม่ต้องบันทึกลงฐานข้อมูล
    public string Password { get; set; }

    public string PasswordHash { get; set; }
    public string Prefix { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Department { get; set; }
    public string Division { get; set; }
    public string Gender { get; set; }
    public string JD { get; set; }
    public string Position { get; set; }
    public string Status { get; set; }
    public string Permission { get; set; }
}
