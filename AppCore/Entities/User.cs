using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AppCore.Entities
{
    [DataContract]
    public class User : BaseEntity
    {
        [DataMember]
        public string Email { get; set; } = string.Empty;

        [DataMember]
        public string DisplayName { get; set; } = string.Empty;
        [DataMember]
        public string FirebaseUid { get; set; } = string.Empty;  // Firebase Auth UID
        
        [DataMember]
        public bool EmailVerified { get; set; }
        [DataMember]
        public string? ProfilePhotoUrl { get; set; }
        [DataMember]
        public string? Bio { get; set; }
        [DataMember]
        public string? Location { get; set; }
        [DataMember]
        public List<string> Skills { get; set; } = new List<string>();
        [DataMember]
        public List<string> Interests { get; set; } = new List<string>();
        [DataMember]
        public List<string> AreasOfExpertise { get; set; } = new List<string>();
        [DataMember]
        public Dictionary<string, string> SocialLinks { get; set; } = new Dictionary<string, string>();  // LinkedIn, GitHub, Twitter, etc.
        public UserPrivacySettings PrivacySettings { get; set; } = UserPrivacySettings.GetDefault();
        [DataMember]
        public UserStatus Status { get; set; }  // Active, Disabled, Suspended
        [DataMember]
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
