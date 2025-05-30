using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO;
using SchoolSystem.Data;
using Microsoft.EntityFrameworkCore;

public class ImageProfileController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly SystemSchoolDbContext _context;

    public ImageProfileController(IWebHostEnvironment env, SystemSchoolDbContext context)
    {
        _env = env;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> UploadProfileImage(UploadProfileImageViewModel model)
    {
       if (model.ProfileImage == null || model.ProfileImage.Length == 0)
        return BadRequest("يرجى اختيار صورة.");

        // التحقق من نوع الملف (MIME type)
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        if (!allowedContentTypes.Contains(model.ProfileImage.ContentType.ToLower()))
            return BadRequest("الملف الذي تم تحميله ليس صورة صالحة.");

        // التحقق من امتداد الملف
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(model.ProfileImage.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
            return BadRequest("امتداد الصورة غير مسموح به.");

        if (model.ProfileImage.Length > 2 * 1024 * 1024)
            return BadRequest("الحد الأقصى لحجم الصورة هو 2 ميجابايت.");

        // تأكد من إنشاء المجلد
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "PrivateImages");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // توليد اسم فريد للصورة
        var fileName = Guid.NewGuid().ToString() + ".webp";
        var filePath = Path.Combine(folderPath, fileName);

        // تحويل الصورة إلى WebP
        using (var image = await Image.LoadAsync(model.ProfileImage.OpenReadStream()))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(512, 512) // اختيار حجم منطقي
            }));

            await image.SaveAsync(filePath, new WebpEncoder());
        }
        ProfileImage imageProfile =await _context.ProfileImages.Where(ip => ip.UserName == model.UserName && ip.Email == model.Email).FirstOrDefaultAsync();
        
        if (imageProfile == null){
            var user = new ProfileImage
            {
                UserName = model.UserName,
                Email = model.Email,
                ProfileImagePath = $"PrivateImages/{fileName}"
            };
            _context.ProfileImages.Add(user);
        }else{
            string lastImage = imageProfile.ProfileImagePath;
            if (!string.IsNullOrEmpty(lastImage))
            {
                // إزالة أي "/" من بداية المسار
                var relativePath = lastImage.TrimStart('/');

                // بناء المسار الفعلي الكامل للصورة
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath); // حذف الصورة
                }
                imageProfile.ProfileImagePath = $"PrivateImages/{fileName}";
            }
            
        }
        

        await _context.SaveChangesAsync();

        return RedirectToAction("IndexProfile","Profile");
    }
}
