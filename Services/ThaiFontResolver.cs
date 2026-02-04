using System;
using System.IO;
using PdfSharpCore.Fonts;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// Custom Font Resolver สำหรับ PdfSharpCore
    /// แก้ปัญหาภาษาไทย (ไม้เอก ไม้โท สระบน ฯลฯ) ไม่แสดงใน PDF
    ///
    /// วิธีใช้:
    /// - วางไฟล์ฟอนต์ .ttf ในโฟลเดอร์ Fonts/
    /// - ถ้ามี Tahoma.ttf / tahomabd.ttf จะใช้ Tahoma
    /// - ถ้าไม่มี จะ fallback ไปใช้ Loma.ttf / Loma-Bold.ttf
    /// </summary>
    public class ThaiFontResolver : IFontResolver
    {
        // Face name keys
        private const string REGULAR_FACE = "Thai#Regular";
        private const string BOLD_FACE = "Thai#Bold";

        private static string _fontsDirectory;

        static ThaiFontResolver()
        {
            // ค้นหาโฟลเดอร์ Fonts จากหลายตำแหน่ง
            string[] searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Fonts"),
                Path.Combine(Directory.GetCurrentDirectory(), "Fonts"),
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
                    "ไม่พบโฟลเดอร์ Fonts/ กรุณาสร้างโฟลเดอร์และวางไฟล์ฟอนต์ .ttf");
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // ทุก font family ที่ร้องขอ จะถูก map ไปที่ฟอนต์ไทยที่ฝังไว้
            string faceName = isBold ? BOLD_FACE : REGULAR_FACE;
            return new FontResolverInfo(faceName);
        }

        public byte[] GetFont(string faceName)
        {
            string fontPath = GetFontFilePath(faceName == BOLD_FACE);

            if (string.IsNullOrEmpty(fontPath) || !File.Exists(fontPath))
            {
                throw new FileNotFoundException(
                    $"ไม่พบไฟล์ฟอนต์ใน {_fontsDirectory}/ " +
                    "กรุณาวางไฟล์ Tahoma.ttf หรือ Loma.ttf");
            }

            return File.ReadAllBytes(fontPath);
        }

        /// <summary>
        /// ค้นหาไฟล์ฟอนต์ โดยเช็ค Tahoma ก่อน ถ้าไม่มีจะ fallback เป็น Loma
        /// </summary>
        private string GetFontFilePath(bool isBold)
        {
            // ลำดับการค้นหา: Tahoma → Loma
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
