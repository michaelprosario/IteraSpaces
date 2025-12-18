using System.Runtime.Serialization;

namespace AppCore.Entities
{
    [DataContract]
    public class UserPrivacySettings
    {
        [DataMember]
        public bool ProfileVisible { get; set; }
        [DataMember]
        public bool ShowEmail { get; set; }
        [DataMember]
        public bool ShowLocation { get; set; }
        [DataMember]
        public bool AllowFollowers { get; set; }
        
        public static UserPrivacySettings GetDefault()
        {
            return new UserPrivacySettings
            {
                ProfileVisible = true,
                ShowEmail = false,
                ShowLocation = true,
                AllowFollowers = true
            };
        }
    }
}
