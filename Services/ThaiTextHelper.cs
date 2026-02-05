using System.Collections.Generic;
using System.Text;

namespace TrainingRequestApp.Services
{
    /// <summary>
    /// แปลง Thai combining marks (ไม้เอก ไม้โท สระบน ฯลฯ) เป็น PUA glyphs
    /// เพื่อให้แสดงผลถูกต้องใน PdfSharpCore ซึ่งไม่รองรับ OpenType GPOS
    ///
    /// หลักการ: ฟอนต์ไทย (Tahoma, Loma ฯลฯ) มี pre-positioned glyphs อยู่ใน
    /// Private Use Area (U+F700-F71F) ซึ่งไม่ต้องอาศัย GPOS ในการจัดตำแหน่ง
    /// </summary>
    public static class ThaiTextHelper
    {
        // Tone marks: standard → PUA (lowered/pre-positioned)
        private const char MAI_EK       = '\u0E48';  // ่
        private const char MAI_THO      = '\u0E49';  // ้
        private const char MAI_TRI      = '\u0E4A';  // ๊
        private const char MAI_CHATTAWA = '\u0E4B';  // ๋
        private const char THANTHAKHAT  = '\u0E4C';  // ์

        private const char PUA_MAI_EK       = '\uF705';
        private const char PUA_MAI_THO      = '\uF706';
        private const char PUA_MAI_TRI      = '\uF707';
        private const char PUA_MAI_CHATTAWA = '\uF708';
        private const char PUA_THANTHAKHAT  = '\uF709';

        // Above-vowels that combine with tone marks
        private const char SARA_I       = '\u0E34';  // ิ
        private const char SARA_II      = '\u0E35';  // ี
        private const char SARA_UE      = '\u0E36';  // ึ
        private const char SARA_UEE     = '\u0E37';  // ื
        private const char MAI_HAN_AKAT = '\u0E31';  // ั

        // PUA: lowered above-vowels (for tall consonants)
        private const char PUA_SARA_I       = '\uF701';
        private const char PUA_SARA_II      = '\uF702';
        private const char PUA_SARA_UE      = '\uF703';
        private const char PUA_SARA_UEE     = '\uF704';

        // PUA: combined above-vowel + tone mark
        private const char PUA_SARA_I_MAI_EK       = '\uF710';
        private const char PUA_SARA_I_MAI_THO      = '\uF711';
        private const char PUA_SARA_I_MAI_TRI      = '\uF712';
        private const char PUA_SARA_I_MAI_CHATTAWA = '\uF713';

        // Tall consonants ที่ต้องใช้ lowered marks
        private static readonly HashSet<char> TallConsonants = new HashSet<char>
        {
            '\u0E1B', // ป
            '\u0E1D', // ฝ
            '\u0E1F', // ฟ
            '\u0E2C', // ฬ
        };

        /// <summary>
        /// แปลง Thai combining marks เป็น PUA pre-positioned glyphs
        /// ข้อความที่ไม่มีอักขระไทยจะคืนค่าเดิมโดยไม่เปลี่ยนแปลง
        /// </summary>
        public static string Fix(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Quick check: skip if no Thai combining marks
            bool hasThai = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '\u0E48' && c <= '\u0E4C')
                {
                    hasThai = true;
                    break;
                }
            }
            if (!hasThai)
                return text;

            var sb = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // Replace tone marks with PUA equivalents
                if (c == MAI_EK || c == MAI_THO || c == MAI_TRI ||
                    c == MAI_CHATTAWA || c == THANTHAKHAT)
                {
                    char puaMark = ToPuaToneMark(c);

                    // Check if preceded by above-vowel → use combined form
                    if (sb.Length > 0)
                    {
                        char prev = sb[sb.Length - 1];
                        char combined = GetCombinedForm(prev, puaMark);
                        if (combined != '\0')
                        {
                            // Replace previous char with combined form
                            sb[sb.Length - 1] = combined;
                            continue;
                        }
                    }

                    sb.Append(puaMark);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static char ToPuaToneMark(char mark)
        {
            switch (mark)
            {
                case MAI_EK:       return PUA_MAI_EK;
                case MAI_THO:      return PUA_MAI_THO;
                case MAI_TRI:      return PUA_MAI_TRI;
                case MAI_CHATTAWA: return PUA_MAI_CHATTAWA;
                case THANTHAKHAT:  return PUA_THANTHAKHAT;
                default:           return mark;
            }
        }

        /// <summary>
        /// ถ้า above-vowel + tone-mark มี combined PUA form ให้คืนค่า combined char
        /// ถ้าไม่มี คืน '\0'
        /// </summary>
        private static char GetCombinedForm(char vowel, char puaMark)
        {
            // Sara I + tone mark → combined PUA
            if (vowel == SARA_I || vowel == PUA_SARA_I)
            {
                switch (puaMark)
                {
                    case PUA_MAI_EK:       return PUA_SARA_I_MAI_EK;
                    case PUA_MAI_THO:      return PUA_SARA_I_MAI_THO;
                    case PUA_MAI_TRI:      return PUA_SARA_I_MAI_TRI;
                    case PUA_MAI_CHATTAWA: return PUA_SARA_I_MAI_CHATTAWA;
                }
            }

            return '\0';
        }
    }
}
