using SchoolSystem.Data;

public interface IAutomaticAccountService
{
    Task<(bool Success, string Message, ApplicationUser? User, string UserName, string InitialPassword, string Email)> CreateAsync(
        int identityNumber, string? email, string role, string? userName = null, string? password = null);
    Task<(bool Success, string Message)> ResetStudentPasswordAsync(Guid managerUserId, int studentIdentityNumber);
}
