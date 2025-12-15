using System;

namespace AppCore.Entities
{
    /// <summary>
    /// Junction table for User-Role many-to-many relationship
    /// </summary>
    public class UserRole : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
