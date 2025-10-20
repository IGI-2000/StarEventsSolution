using System.Threading.Tasks;
using StarEvents.Models.ViewModels;

namespace StarEvents.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(LoginViewModel model);
        Task<OperationResult> RegisterCustomerAsync(RegisterViewModel model);
        void SignOut();
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}