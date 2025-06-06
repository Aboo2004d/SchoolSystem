using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SchoolSystem.Data;

public class ExportDataController : Controller
{
    private readonly SystemSchoolDbContext _context;
    private readonly INotyfService _notyf;
    private readonly IErrorLoggerService _logger;

    public ExportDataController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger)
    {
        _context = context;
        _notyf = notyf;
        _logger = logger;
    }

    public IActionResult ExportAllStudentInSchoolToExcel()
    {
        try
        {
            ExcelPackage.License.SetNonCommercialOrganization("وزارة التربية والتعليم العالي فلسطين");
            var students = _context.Students.Where(s => s.IdSchool == HttpContext.Session.GetInt32("School"))
            .Include(s => s.IdClassNavigation).Include(s => s.Grades)
            .Include(s => s.Attendances).Include(s => s.IdSchoolNavigation).ToList(); // جلب بيانات الطلاب من قاعدة البيانات

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("الطلاب في المدرسة");

            // إضافة رؤوس الأعمدة
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "الاسم";
            worksheet.Cells[1, 3].Value = "رقم الهوية";
            worksheet.Cells[1, 4].Value = "رقم الهاتف";
            worksheet.Cells[1, 5].Value = "البريد الإلكتروني";
            worksheet.Cells[1, 6].Value = "تاريخ الميلاد";
            worksheet.Cells[1, 7].Value = "العنوان";
            worksheet.Cells[1, 8].Value = "الصف الدراسي";
            worksheet.Cells[1, 9].Value = "المعدل";
            worksheet.Cells[1, 10].Value = "الحضور";
            worksheet.Cells[1, 11].Value = "المدرسة";

            // تعبئة البيانات
            int row = 2;
            foreach (var student in students)
            {
                worksheet.Cells[row, 1].Value = row - 1; // الرقم التسلسلي
                worksheet.Cells[row, 2].Value = student.Name;
                worksheet.Cells[row, 3].Value = student.IdNumber;
                worksheet.Cells[row, 4].Value = student.Phone;
                worksheet.Cells[row, 5].Value = student.Email;
                // تعيين تنسيق التاريخ للخلية
                worksheet.Cells[row, 6].Value = student.TheDate;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "yyyy-mm-dd";

                worksheet.Cells[row, 7].Value = student.City + "/" + student.Area;
                worksheet.Cells[row, 8].Value = student.IdClassNavigation?.Name ?? "غير معرف";
                worksheet.Cells[row, 9].Value = student.Grades.Select(g => g.Total).Average() ?? 0; // حساب المعدل
                worksheet.Cells[row, 10].Value = student.Attendances.Count(att => att.AttendanceStatus == "1") + "/" + student.Attendances.Count; // حساب الحضور
                worksheet.Cells[row, 11].Value = student.IdSchoolNavigation?.Name ?? "غير معرف";
                row++;
            }

            // إعدادات العرض
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileName = "الطلاب.xlsx";
            var fileContent = package.GetAsByteArray();



            // إرجاع الملف للتنزيل
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _notyf.Error("حدث خطأ أثناء تصدير البيانات. يرجى المحاولة مرة أخرى.");
            _logger.LogAsync(ex, "ExportData/ExportAllStudentInSchoolToExcel");
            return RedirectToAction("ManagerMenegarStudentView", "Menegar");
        }
    }
    
    public IActionResult ExportAllTeacherInSchoolToExcel()
    {
        try
        {
            ExcelPackage.License.SetNonCommercialOrganization("وزارة التربية والتعليم العالي فلسطين");
            var teachers = _context.Teachers.Where(s => s.IdSchool == HttpContext.Session.GetInt32("School"))
            .Include(s => s.IdSchoolNavigation).ToList(); // جلب بيانات المعلمين من قاعدة البيانات

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("المعلمين في المدرسة");

            // إضافة رؤوس الأعمدة
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "الاسم";
            worksheet.Cells[1, 3].Value = "رقم الهوية";
            worksheet.Cells[1, 4].Value = "رقم الهاتف";
            worksheet.Cells[1, 5].Value = "البريد الإلكتروني";
            worksheet.Cells[1, 6].Value = "تاريخ الميلاد";
            worksheet.Cells[1, 7].Value = "العنوان";
            worksheet.Cells[1, 8].Value = "المدرسة";

            // تعبئة البيانات
            int row = 2;
            foreach (var teacher in teachers)
            {
                worksheet.Cells[row, 1].Value = row - 1; // الرقم التسلسلي
                worksheet.Cells[row, 2].Value = teacher.Name;
                worksheet.Cells[row, 3].Value = teacher.IdNumber;
                worksheet.Cells[row, 4].Value = teacher.Phone;
                worksheet.Cells[row, 5].Value = teacher.Email;
                // تعيين تنسيق التاريخ للخلية
                worksheet.Cells[row, 6].Value = teacher.TheDate;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "yyyy-mm-dd";

                worksheet.Cells[row, 7].Value = $"{teacher.City} / {teacher.Area}";
                worksheet.Cells[row, 8].Value = teacher.IdSchoolNavigation?.Name ?? "غير معرف";
                row++;
            }

            // إعدادات العرض
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileName = "المعلمين.xlsx";
            var fileContent = package.GetAsByteArray();



            // إرجاع الملف للتنزيل
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _notyf.Error("حدث خطأ أثناء تصدير البيانات. يرجى المحاولة مرة أخرى.");
            _logger.LogAsync(ex, "ExportData/ExportAllTeacherInSchoolToExcel");
            return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
        }
    }
    
    public IActionResult ExportAllStudentInTeacherToExcel(int idTeacher)
    {
        try
        {
            ExcelPackage.License.SetNonCommercialOrganization("وزارة التربية والتعليم العالي فلسطين");
            var studentsInTeacher = _context.StudentLectuerTeachers
            .Where(s => s.IdSchool == HttpContext.Session.GetInt32("School") && s.IdTeacher == idTeacher)
            .Include(s => s.IdLectuerNavigation)
            .Include(s => s.IdStudentNavigation).Include(s => s.IdTeacherNavigation).ToList(); // جلب بيانات المعلمين من قاعدة البيانات

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("الطلاب لدى المعلم");

            // إضافة رؤوس الأعمدة
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "اسم الطالب";
            worksheet.Cells[1, 3].Value = "المادة";
            worksheet.Cells[1, 4].Value = "الحضور";
            worksheet.Cells[1, 5].Value = "العلامة";

            // تعبئة البيانات
            int row = 2;
            foreach (var student in studentsInTeacher)
            {
                worksheet.Cells[row, 1].Value = row - 1; // الرقم التسلسلي
                worksheet.Cells[row, 2].Value = student.IdStudentNavigation?.Name??"غير معرف";
                worksheet.Cells[row, 3].Value = student.IdLectuerNavigation?.Name??"غير معرف";
                worksheet.Cells[row, 4].Value = _context.Attendances.Where(
                    sa => sa.IdTeacher == idTeacher && sa.IdLectuer == student.IdLectuer &&  sa.IdStudent == student.IdStudent
                ).Count(a => a.AttendanceStatus == "1") + "/" + _context.Attendances.Where(
                    sa => sa.IdTeacher == idTeacher && sa.IdLectuer == student.IdLectuer &&  sa.IdStudent == student.IdStudent
                ).Count();
                worksheet.Cells[row, 5].Value =  _context.Grades.Where(
                    sa => sa.IdTeacher == idTeacher && sa.IdLectuer == student.IdLectuer &&  sa.IdStudent == student.IdStudent
                ).Select(g => g.Total).SingleOrDefault() + "/100";
                row++;
            }

            // إعدادات العرض
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileName = $"الطلاب لدى المعلم {studentsInTeacher[0].IdTeacherNavigation?.Name??"غير معرف"}.xlsx";
            var fileContent = package.GetAsByteArray();



            // إرجاع الملف للتنزيل
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogAsync(ex, "ExportData/ExportAllStudentInTeacherToExcel");
            _notyf.Error("حدث خطأ أثناء تصدير البيانات. يرجى المحاولة مرة أخرى.");
            return RedirectToAction("Students", "Teacher");
        }
    }
    
    public IActionResult ExportGradesStudentsToExcel(int idTeacher)
    {
        try
        {
            ExcelPackage.License.SetNonCommercialOrganization("وزارة التربية والتعليم العالي فلسطين");
            var studentsGrades = _context.Grades
            .Where(s => s.IdSchool == HttpContext.Session.GetInt32("School") && s.IdTeacher == idTeacher)
            .Include(s => s.IdLectuerNavigation)
            .Include(s => s.IdStudentNavigation).Include(s => s.IdTeacherNavigation).ToList(); // جلب بيانات المعلمين من قاعدة البيانات

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("علامات الطلاب");

            // إضافة رؤوس الأعمدة
            worksheet.Cells[1, 1].Value = "#";
            worksheet.Cells[1, 2].Value = "اسم الطالب";
            worksheet.Cells[1, 3].Value = "المادة";
            worksheet.Cells[1, 4].Value = "الشهر الاول";
            worksheet.Cells[1, 5].Value = "النصفي";
            worksheet.Cells[1, 6].Value = "الشهر التاني";
            worksheet.Cells[1, 7].Value = "النشاط";
            worksheet.Cells[1, 8].Value = "النهائي";
            worksheet.Cells[1, 9].Value = "المجموع";

            // تعبئة البيانات
            int row = 2;
            foreach (var student in studentsGrades)
            {
                worksheet.Cells[row, 1].Value = row - 1; // الرقم التسلسلي
                worksheet.Cells[row, 2].Value = student.IdStudentNavigation?.Name ?? "غير معرف";
                worksheet.Cells[row, 3].Value = student.IdLectuerNavigation?.Name ?? "غير معرف";
                worksheet.Cells[row, 4].Value = student.FirstMonth;
                worksheet.Cells[row, 5].Value = student.Mid;
                worksheet.Cells[row, 6].Value = student.SecondMonth;
                worksheet.Cells[row, 7].Value = student.Activity;
                worksheet.Cells[row, 8].Value = student.Final;
                worksheet.Cells[row, 9].Value = student.Total;
                row++;
            }

            // إعدادات العرض
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileName = $"علامات الطلاب لدى المعلم {studentsGrades[0].IdTeacherNavigation?.Name ?? "غير معرف"}.xlsx";
            var fileContent = package.GetAsByteArray();



            // إرجاع الملف للتنزيل
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogAsync(ex, "ExportData/ExportAllStudentInTeacherToExcel");
            _notyf.Error("حدث خطأ أثناء تصدير البيانات. يرجى المحاولة مرة أخرى.");
            return RedirectToAction("Students", "Teacher");
        }
    }


}
