using System;
using System.Collections.Generic;

namespace AppCore.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FirebaseUid { get; set; } = string.Empty;  // Firebase Auth UID
        public bool EmailVerified { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> Interests { get; set; } = new List<string>();
        public List<string> AreasOfExpertise { get; set; } = new List<string>();
        public Dictionary<string, string> SocialLinks { get; set; } = new Dictionary<string, string>();  // LinkedIn, GitHub, Twitter, etc.
        public UserPrivacySettings PrivacySettings { get; set; } = UserPrivacySettings.GetDefault();
        public UserStatus Status { get; set; }  // Active, Disabled, Suspended
        public DateTime? LastLoginAt { get; set; }
    }

    public enum UserStatus
    {
        Active,
        Disabled,
        Suspended,
        PendingVerification
    }
}
