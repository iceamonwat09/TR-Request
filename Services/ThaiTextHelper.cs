using System.Collections.Generic;
using System.Text;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// แปลง Thai combining marks (ไม้เอก ไม้โท สระบน ฯลฯ) เป็น PUA glyphs
    /// เพื่อให้แสดงผลถูกต้องใน PdfSharpCore ซึ่งไม่รองรับ OpenType GPOS
    ///
    /// สำหรับฟอนต์ Loma: ใช้ LOW variants (U+F70A-F70E) สำหรับวรรณยุกต์
    /// ที่ตามหลังสระบน (ิ ี ึ ื ั) เพื่อไม่ให้ซ้อนทับกัน
    /// </summary>
    public static class ThaiTextHelper
    {
        // Standard Thai tone marks
        private const char MAI_EK       = '\u0E48';  // ่
        private const char MAI_THO      = '\u0E49';  // ้
        private const char MAI_TRI      = '\u0E4A';  // ๊
        private const char MAI_CHATTAWA = '\u0E4B';  // ๋
        private const char THANTHAKHAT  = '\u0E4C';  // ์

        // Loma PUA: LOW variants of tone marks (ใช้เมื่อมีสระบนนำหน้า)
        private const char PUA_MAI_EK_LOW       = '\uF70A';
        private const char PUA_MAI_THO_LOW      = '\uF70B';
        private const char PUA_MAI_TRI_LOW      = '\uF70C';
        private const char PUA_MAI_CHATTAWA_LOW = '\uF70D';
        private const char PUA_THANTHAKHAT_LOW  = '\uF70E';

        // Above-vowels ที่ทำให้ต้องใช้ LOW variant ของวรรณยุกต์
        private static readonly HashSet<char> AboveVowels = new HashSet<char>
        {
            '\u0E31', // ั MAI HAN AKAT
            '\u0E34', // ิ SARA I
            '\u0E35', // ี SARA II
            '\u0E36', // ึ SARA UE
            '\u0E37', // ื SARA UEE
            '\u0E47', // ็ MAITAIKHU
            '\u0E4D', // ํ NIKHAHIT
        };

        /// <summary>
        /// แปลง Thai combining marks เป็น PUA glyphs ที่เหมาะสมกับ Loma font
        /// - วรรณยุกต์ที่ตามหลังสระบน → ใช้ LOW variant (F70A-F70E)
        /// - วรรณยุกต์เดี่ยว → คงเดิม
        /// </summary>
        public static string Fix(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Quick check: skip if no Thai tone marks
            bool hasToneMark = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= MAI_EK && c <= THANTHAKHAT)
                {
                    hasToneMark = true;
                    break;
                }
            }
            if (!hasToneMark)
                return text;

            var sb = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Check if this is a tone mark
                if (c >= MAI_EK && c <= THANTHAKHAT)
                {
                    // Check if preceded by an above-vowel → use LOW variant
                    bool uselow = false;
                    if (sb.Length > 0)
                    {
                        char prev = sb[sb.Length - 1];
                        if (AboveVowels.Contains(prev))
                        {
                            uselow = true;
                        }
                    }

                    if (uselow)
                    {
                        // Use Loma PUA LOW variant
                        sb.Append(ToPuaLowToneMark(c));
                    }
                    else
                    {
                        // Keep standard tone mark
                        sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// แปลงวรรณยุกต์เป็น LOW variant (Loma PUA)
        /// </summary>
        private static char ToPuaLowToneMark(char mark)
        {
            switch (mark)
            {
                case MAI_EK:       return PUA_MAI_EK_LOW;
                case MAI_THO:      return PUA_MAI_THO_LOW;
                case MAI_TRI:      return PUA_MAI_TRI_LOW;
                case MAI_CHATTAWA: return PUA_MAI_CHATTAWA_LOW;
                case THANTHAKHAT:  return PUA_THANTHAKHAT_LOW;
                default:           return mark;
            }
        }
    }
}
