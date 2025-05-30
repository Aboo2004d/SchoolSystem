public interface IEmailValidationService
{
    Task<bool> IsEmailValidAsync(string email);
}
