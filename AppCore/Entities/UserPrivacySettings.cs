namespace AppCore.Entities
{
    public class UserPrivacySettings
    {
        public bool ProfileVisible { get; set; }
        public bool ShowEmail { get; set; }
        public bool ShowLocation { get; set; }
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
