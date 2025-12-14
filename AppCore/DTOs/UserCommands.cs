using System.Collections.Generic;
using AppCore.Entities;

namespace AppCore.DTOs
{
    public class RegisterUserCommand
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;
    }

    public class UpdateUserProfileCommand
    {
        public string UserId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public List<string>? Skills { get; set; }
        public List<string>? Interests { get; set; }
        public List<string>? AreasOfExpertise { get; set; }
        public Dictionary<string, string>? SocialLinks { get; set; }
    }

    public class UpdatePrivacySettingsCommand
    {
        public string UserId { get; set; } = string.Empty;
        public UserPrivacySettings PrivacySettings { get; set; } = UserPrivacySettings.GetDefault();
    }

    public class DisableUserCommand
    {
        public string UserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string DisabledBy { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
    }
}
