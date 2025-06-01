using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO;
using SchoolSystem.Data;
using Microsoft.EntityFrameworkCore;
using AspNetCoreHero.ToastNotification.Abstractions;
using SchoolSystem.Filters;

public class ImageProfileController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly SystemSchoolDbContext _context;
    private readonly INotyfService _notyf;

    public ImageProfileController(IWebHostEnvironment env, INotyfService notyf, SystemSchoolDbContext context)
    {
        _env = env;
        _context = context;
        _notyf = notyf;
    }

    [HttpPost]
    [AuthorizeRoles("admin", "Student", "Teacher")]
    public async Task<IActionResult> UploadProfileImage(UploadProfileImageViewModel model)
    {
        if (model.ProfileImage == null || model.ProfileImage.Length == 0)
        {
            _notyf.Error("يرجى اختيار صورة.");
            return RedirectToAction("IndexProfile", "Profile");
        }

        // التحقق من نوع الملف (MIME type)
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        if (!allowedContentTypes.Contains(model.ProfileImage.ContentType.ToLower()))
        {
            _notyf.Error("الملف الذي تم تحميله ليس صورة صالحة.");
            return RedirectToAction("IndexProfile", "Profile");
        }

        // التحقق من امتداد الملف
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(model.ProfileImage.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            _notyf.Error("امتداد الصورة غير مسموح به.");
            return RedirectToAction("IndexProfile", "Profile");

        }

        if (model.ProfileImage.Length > 2 * 1024 * 1024)
        {
            _notyf.Error("الحد الأقصى لحجم الصورة هو 2 ميجابايت.");
            return RedirectToAction("IndexProfile", "Profile");
        }

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
        ProfileImage? imageProfile = await _context.ProfileImages.SingleOrDefaultAsync(ip => ip.UserName == model.UserName);

        if (imageProfile == null)
        {
            var user = new ProfileImage
            {
                UserName = model.UserName,
                Email = model.Email,
                ProfileImagePath = $"PrivateImages/{fileName}"
            };
            _context.ProfileImages.Add(user);
        }
        else
        {
            string lastImage = imageProfile?.ProfileImagePath ?? string.Empty;
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
        _notyf.Success("تم تحميل الصورة بنجاح.");
        return RedirectToAction("IndexProfile", "Profile");
    }
}
