using System;
using System.IO;
using PdfSharpCore.Fonts;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// Custom Font Resolver สำหรับ PdfSharpCore
    /// แก้ปัญหาภาษาไทย (ไม้เอก ไม้โท สระบน ฯลฯ) ไม่แสดงใน PDF
    ///
    /// ใช้ฟอนต์ Loma เป็นหลัก เพราะมี PUA glyphs (U+F700-F71F)
    /// สำหรับ pre-positioned tone marks ที่ทำงานร่วมกับ ThaiTextHelper
    ///
    /// ไฟล์ที่ต้องมีใน Fonts/:
    ///   - Loma.ttf และ Loma-Bold.ttf (ฝังมากับโปรเจค)
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
                    "กรุณาวาง Loma.ttf + Loma-Bold.ttf ในโฟลเดอร์ Fonts/");
            }

            return File.ReadAllBytes(fontPath);
        }

        /// <summary>
        /// ค้นหาฟอนต์ตามลำดับ: Loma (มี PUA glyphs สำหรับไม้เอก ไม้โท) → tahoma
        /// </summary>
        private static string FindFont(bool isBold)
        {
            // Loma มี PUA glyphs (U+F700-F71F) สำหรับ pre-positioned tone marks
            // Tahoma จาก Windows ไม่มี PUA glyphs จึงต้องใช้ Loma ก่อน
            string[] candidates = isBold
                ? new[] { "Loma-Bold.ttf", "tahomabd.ttf", "Tahoma-Bold.ttf" }
                : new[] { "Loma.ttf", "tahoma.ttf", "Tahoma.ttf" };

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

        public string DefaultFontName => "Loma";
    }
}
