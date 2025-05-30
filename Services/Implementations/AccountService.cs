using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolSystem.Data;
using SchoolSystem.Models;
using System.Threading.Tasks;

public class AccountService : IAccountService
{
    private readonly SystemSchoolDbContext _context;
    private readonly ILogger<AccountService> _logger;

    private readonly IEmailValidationService _emailValidator;

    public AccountService(SystemSchoolDbContext context,
                ILogger<AccountService> logger,
                IEmailValidationService emailValidator)
    {
        _context = context;
        _logger = logger;
        _emailValidator = emailValidator;
    }

    public async Task<OperationResult> RegisterUserAsync(RegisterViewModel model)
    {
        try
        {
            var result = await UserFoundAsync(model);
            if (!result.IsSuccess)
            {
                return Fail(result.Message);
            }
            
            return Success(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تسجيل المستخدم.");
            return Fail( "حدث خطأ أثناء إنشاء الحساب.");
        }
    }
    public async Task<OperationResult> UserFoundAsync(RegisterViewModel model)
    {
        try
        {
            var result = await ValidateUserDataAsync(model);
            if (!result.IsSuccess)
            {
                return Fail(result.Message);
                
            }

            switch (model.Role.Trim().ToLower())
            {
                case "admin":
                    Menegar? menegar =await _context.Menegars.FirstOrDefaultAsync(m => m.IdNumber == model.IdNumber );
                    if (menegar == null)
                    {
                        return Fail("رقم الهوية غير صالح للمشرف.");
                    }
                    if (!IsUserDataMatching(menegar.Name,menegar.Email,menegar.TheDate??DateOnly.MinValue,menegar.City,menegar.Area, model))
                    {
                        return Fail("تحقق من البيانات المدخلة للمدير");
                    }
                    model.IdUser = menegar.Id;
                    model.School = menegar.IdSchool;
                    break;
                case "teacher":
                    Teacher? teacher =await _context.Teachers.FirstOrDefaultAsync(t => t.IdNumber == model.IdNumber );
                    if (teacher == null)
                    {
                        return Fail("رقم الهوية غير صالح للمعلم.");
                    }
                    if (!IsUserDataMatching(teacher.Name,teacher.Email,teacher.TheDate??DateOnly.MinValue,teacher.City,teacher.Area, model))
                    {
                        return Fail( "تحقق من البيانات المدخلة للمعلم");
                    }
                    model.IdUser = teacher.Id;
                    model.School = teacher.IdSchool;
                    break;
                case "student":
                    Student? student =await _context.Students.FirstOrDefaultAsync(t => t.IdNumber == model.IdNumber );
                    if (student == null)
                    {
                        
                        return Fail( "رقم الهوية غير صالح للطالب.");
                    }
                    if (!IsUserDataMatching(student.Name,student.Email,student.TheDate??DateOnly.MinValue,student.City,student.Area, model))
                    {
                        return Fail("تحقق من البيانات المدخلة للطالب");
                    }
                    model.IdUser = student.Id;
                    model.School = student.IdSchool;
                    break;
                default:
                {
                    return Fail("الدور غير صالح.");
                }

            }
            return Success("بيانات صحيحة.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تسجيل المستخدم.");
            return Fail("خطأ اثناء التحقق من البيانات.");
        }
    }
    public async Task<OperationResult> ValidateUserDataAsync(RegisterViewModel model)
    {
        try
        {
            // التحقق من الاسم
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return Fail("الاسم مطلوب.");
                
            }

            // التحقق من رقم الهوية (يجب أن يكون 9 أرقام)
            if (model.IdNumber.ToString().Length != 9)
            {
                return Fail("رقم الهوية يجب أن يتكون من 9 أرقام.");
            }

            // التحقق من رقم الهاتف (بسيط - يمكن تخصيصه لاحقًا)
            if (string.IsNullOrWhiteSpace(model.Phone) || model.Phone.Length < 8)
            {
                return Fail("رقم الهاتف غير صالح.");
            }

            // التحقق من البريد الإلكتروني عبر الخدمة الخارجية
            if (!await _emailValidator.IsEmailValidAsync(model.Email))
            {
                return Fail("البريد الإلكتروني غير صالح.");
            }

            // التحقق من المدينة
            if (string.IsNullOrWhiteSpace(model.City))
            {
                return Fail("المدينة مطلوبة.");
            }

            // التحقق من الحي
            if (string.IsNullOrWhiteSpace(model.Area))
            {
                return Fail("الحي مطلوب.");
            }

            // التحقق من تاريخ الميلاد (يجب أن يكون تاريخًا صحيحًا وماضيًا)
            var today = DateOnly.FromDateTime(DateTime.Today);
            int age = today.Year - model.TheDate.Year;
            if (model.TheDate == default || model.TheDate > today || (model.TheDate.AddYears(age) > today) || age < 5)
            {
                return Fail("تاريخ الميلاد غير صالح.");
            }

            return Success("جميع البيانات صحيحة.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء التحقق من بيانات المستخدم.");
            return Fail("خطأ اثناء التحقق من البيانات.");
        }
    }

    private bool IsUserDataMatching(string? user_Name,string? user_Email,DateOnly user_TheDate,string? user_City,string? user_Area, RegisterViewModel model)
    {
        bool emailsMatch = string.Equals(user_Email, model.Email);
        bool namesMatch = string.Equals(user_Name, model.FullName);
        bool datesMatch = user_TheDate == model.TheDate;
        bool citiesMatch = string.Equals(user_City, model.City);
        bool areasMatch = string.Equals(user_Area, model.Area);

        return emailsMatch && namesMatch && datesMatch && citiesMatch && areasMatch;
    }

    private OperationResult Fail(string message) =>
    new OperationResult { IsSuccess = false, Message = message };

    private OperationResult Success(string message) =>
        new OperationResult { IsSuccess = true, Message = message };
}
