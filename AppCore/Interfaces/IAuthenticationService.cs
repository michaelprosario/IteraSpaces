using System.Threading.Tasks;
using AppCore.DTOs;

namespace AppCore.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResult> VerifyFirebaseTokenAsync(string idToken);
        Task<AuthResult> CreateFirebaseUserAsync(string email, string password);
        Task<AuthResult> UpdateFirebaseUserAsync(string uid, UpdateUserRequest request);
        Task<bool> SendPasswordResetEmailAsync(string email);
        Task<bool> SendEmailVerificationAsync(string uid);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Uid { get; set; }
        public string? Email { get; set; }
        public bool EmailVerified { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
