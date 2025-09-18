using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TrainingRequestApp.Models;
using TrainingRequestApp.Services;
using Microsoft.AspNetCore.Builder;
using System;

var builder = WebApplication.CreateBuilder(args);

// ✅ เพิ่ม ConfigurationManager รองรับ .NET 6+
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ✅ เชื่อมต่อฐานข้อมูลด้วย DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ เพิ่ม Session Service เพื่อจัดการข้อมูลผู้ใช้ที่ล็อกอิน
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // กำหนดอายุ Session เป็น 30 นาที
    options.Cookie.HttpOnly = true; // ป้องกันการเข้าถึงจาก JavaScript
    options.Cookie.IsEssential = true; // ทำให้ Session จำเป็นสำหรับการทำงานของแอป
});

// ✅ ลงทะเบียน Services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ITrainingRequestService, TrainingRequestService>();

// ✅ เพิ่ม Controllers และ Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ เปิดใช้งาน Session Middleware เพื่อรองรับข้อมูลล็อกอิน
app.UseSession();

app.UseAuthorization();

// ✅ ปรับ Routing: เมื่อผู้ใช้ล็อกอินสำเร็จ ให้ไปที่หน้า Home
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// ✅ เพิ่ม Routing สำหรับหน้า Home (Dashboard)
app.MapControllerRoute(
    name: "home",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
