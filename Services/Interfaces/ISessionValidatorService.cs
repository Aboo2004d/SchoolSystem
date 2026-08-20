public interface ISessionValidatorService
{
    Task<(bool IsValid, Guid IdTeacher, Guid IdSchool , bool status)> ValidateTeacherSessionAsync(HttpContext httpContext, Guid teacherId, string sours);
    Task<(bool IsValid, Guid IdTeacher, Guid IdSchool , bool status)> ValidateStudentSessionAsync(HttpContext httpContext, Guid studentId, string sours);
    Task<(bool IsValid, Guid IdStudent, Guid IdSchool, bool status)> ValidateStudentDataAccessAsync(HttpContext httpContext, Guid studentId, string source);
    Task<(bool IsValid, Guid IdSchool, string Message)> ValidateManagerSessionAsync(HttpContext httpContext, string sours);
    Task<(bool IsValid, Guid DirectorateId, string Message)> ValidateDirectorateManagerSessionAsync(HttpContext httpContext, string source);
    Task<(bool IsValid, Guid DirectorateId, string Message)> ValidateDirectorateSchoolAccessAsync(HttpContext httpContext, Guid schoolId, string source);
}
