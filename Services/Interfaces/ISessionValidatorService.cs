public interface ISessionValidatorService
{
    Task<(bool IsValid, Guid IdTeacher, Guid IdSchool , bool status)> ValidateTeacherSessionAsync(HttpContext httpContext, Guid teacherId, string sours);
    Task<(bool IsValid, Guid IdTeacher, Guid IdSchool , bool status)> ValidateStudentSessionAsync(HttpContext httpContext, Guid studentId, string sours);
    Task<(bool IsValid, Guid IdSchool, string Message)> ValidateManagerSessionAsync(HttpContext httpContext, string sours);
}
