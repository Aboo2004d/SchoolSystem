using System.Threading.Tasks;
using SchoolSystem.Models;

public interface IAccountService
{
    Task<OperationResult> RegisterUserAsync(RegisterViewModel model);
    Task<OperationResult> UserFoundAsync(RegisterViewModel model);
    Task<OperationResult> ValidateUserDataAsync(RegisterViewModel model);
}

