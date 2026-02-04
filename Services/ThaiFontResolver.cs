using System;
using System.IO;
using PdfSharpCore.Fonts;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// Custom Font Resolver สำหรับ PdfSharpCore
    /// แก้ปัญหาภาษาไทย (ไม้เอก ไม้โท สระบน ฯลฯ) ไม่แสดงใน PDF
    ///
    /// วิธีใช้ Tahoma:
    ///   1. คัดลอกไฟล์จาก Windows: C:\Windows\Fonts\tahoma.ttf และ tahomabd.ttf
    ///   2. วางในโฟลเดอร์: [โปรเจค]/Fonts/tahoma.ttf และ Fonts/tahomabd.ttf
    ///   3. ระบบจะใช้ Tahoma โดยอัตโนมัติ
    ///
    /// ถ้าไม่มี Tahoma จะ fallback ใช้ Loma.ttf (ฟอนต์ไทยที่ฝังมาด้วย)
    /// </summary>
    public class ThaiFontResolver : IFontResolver
    {
        private const string REGULAR_FACE = "Thai#Regular";
        private const string BOLD_FACE = "Thai#Bold";

        private static string _fontsDirectory;
        private static string _resolvedRegularFont;
        private static string _resolvedBoldFont;

        static ThaiFontResolver()
        {
            // ค้นหาโฟลเดอร์ Fonts จากหลายตำแหน่ง
            string[] searchPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Fonts"),
                Path.Combine(AppContext.BaseDirectory, "Fonts"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts")
            };

            _fontsDirectory = null;
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    _fontsDirectory = path;
                    break;
                }
            }

            if (_fontsDirectory == null)
            {
                throw new DirectoryNotFoundException(
                    "ไม่พบโฟลเดอร์ Fonts/ กรุณาสร้างโฟลเดอร์ Fonts/ ในโปรเจค");
            }

            // ค้นหาฟอนต์ล่วงหน้า และแจ้งว่าใช้ฟอนต์อะไร
            _resolvedRegularFont = FindFont(false);
            _resolvedBoldFont = FindFont(true);

            Console.WriteLine($"[ThaiFontResolver] Fonts directory: {_fontsDirectory}");
            Console.WriteLine($"[ThaiFontResolver] Regular: {Path.GetFileName(_resolvedRegularFont ?? "NOT FOUND")}");
            Console.WriteLine($"[ThaiFontResolver] Bold: {Path.GetFileName(_resolvedBoldFont ?? "NOT FOUND")}");
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(isBold ? BOLD_FACE : REGULAR_FACE);
        }

        public byte[] GetFont(string faceName)
        {
            string fontPath = (faceName == BOLD_FACE) ? _resolvedBoldFont : _resolvedRegularFont;

            if (string.IsNullOrEmpty(fontPath) || !File.Exists(fontPath))
            {
                throw new FileNotFoundException(
                    $"ไม่พบไฟล์ฟอนต์ใน {_fontsDirectory}/ " +
                    "กรุณาวาง tahoma.ttf + tahomabd.ttf (คัดลอกจาก C:\\Windows\\Fonts\\) " +
                    "หรือ Loma.ttf + Loma-Bold.ttf ในโฟลเดอร์ Fonts/");
            }

            return File.ReadAllBytes(fontPath);
        }

        /// <summary>
        /// ค้นหาฟอนต์ตามลำดับ: tahoma → Loma
        /// </summary>
        private static string FindFont(bool isBold)
        {
            string[] candidates = isBold
                ? new[] { "tahomabd.ttf", "Tahoma-Bold.ttf", "Loma-Bold.ttf" }
                : new[] { "tahoma.ttf", "Tahoma.ttf", "Loma.ttf" };

            foreach (var fileName in candidates)
            {
                string fullPath = Path.Combine(_fontsDirectory, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        public string DefaultFontName => "Tahoma";
    }
}
