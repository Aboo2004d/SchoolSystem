using Microsoft.AspNetCore.Mvc;

namespace SchoolSystem.Controllers
{
    public class ImageController : Controller
    {
        private readonly string _imageRootPath;

        public ImageController(IWebHostEnvironment env)
        {
            // تحديد المسار الكامل لمجلد الصور الخاصة
            _imageRootPath = Path.Combine(env.ContentRootPath, "PrivateImages");
        }

        [HttpGet]
        public IActionResult GetImage(string fileName)
        {
            if (fileName.Contains("..") || Path.GetFileName(fileName) != fileName)
            {
                return BadRequest("Invalid file name.");
            }
            // تحقق من تسجيل دخول المستخدم (يمكنك تخصيصها بحسب مشروعك)
            int Id = HttpContext.Session.GetInt32("Id")??0;
            if (Id == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var filePath = Path.Combine(_imageRootPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            // تحديد نوع الصورة تلقائيًا
            var contentType = GetContentType(filePath);
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType);
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
