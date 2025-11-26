using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace TrainingRequestApp.Filters
{
    /// <summary>
    /// Action Filter เพื่อตรวจสอบว่า User มี Session (Login แล้ว) หรือไม่
    /// ถ้าไม่มี Session → Redirect ไป Login พร้อมบันทึก ReturnUrl
    ///
    /// วิธีใช้งาน:
    /// [RequireSession]
    /// public async Task<IActionResult> Edit(string docNo) { ... }
    /// </summary>
    public class RequireSessionAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// ชื่อ Session Key ที่เก็บ Email ของ User
        /// </summary>
        public string SessionKey { get; set; } = "UserEmail";

        /// <summary>
        /// Login Route (Controller/Action)
        /// Default: /Account/Login
        /// </summary>
        public string LoginRoute { get; set; } = "/Account/Login";

        /// <summary>
        /// ข้อความที่จะแสดงหลัง Redirect ไป Login
        /// </summary>
        public string Message { get; set; } = "กรุณาล็อกอินเพื่อดำเนินการต่อ";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var session = httpContext.Session;

            // ดึง UserEmail จาก Session
            string userEmail = session.GetString(SessionKey);

            // เช็คว่ามี Session หรือไม่
            if (string.IsNullOrEmpty(userEmail))
            {
                // บันทึก ReturnUrl เพื่อกลับมาหลัง Login
                var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;

                // บันทึกใน TempData (จะหายหลังใช้งาน 1 ครั้ง)
                var tempData = context.Controller is Controller controller
                    ? controller.TempData
                    : null;

                if (tempData != null)
                {
                    tempData["ReturnUrl"] = returnUrl.ToString();
                    tempData["Info"] = Message;
                }

                // Log
                System.Console.WriteLine($"⚠️ RequireSession: No session found");
                System.Console.WriteLine($"   SessionKey: {SessionKey}");
                System.Console.WriteLine($"   ReturnUrl: {returnUrl}");
                System.Console.WriteLine($"   Redirecting to: {LoginRoute}");

                // Redirect ไป Login
                context.Result = new RedirectResult(LoginRoute);
                return;
            }

            // มี Session → ดำเนินการต่อ
            System.Console.WriteLine($"✅ RequireSession: Session found - UserEmail: {userEmail}");
            base.OnActionExecuting(context);
        }
    }
}
