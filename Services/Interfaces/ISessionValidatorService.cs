public interface ISessionValidatorService
{
    Task<(bool IsValid, int IdTeacher, int IdSchool , bool status)> ValidateTeacherSessionAsync(HttpContext httpContext, int teacherId, string sours);
    Task<(bool IsValid, int IdTeacher, int IdSchool , bool status)> ValidateStudentSessionAsync(HttpContext httpContext, int studentId, string sours);
    Task<(bool IsValid, int IdSchool , bool status)> ValidateAdminSessionAsync(HttpContext httpContext, string sours);
}
