using System.Collections.Generic;

namespace AppCore.Entities
{
    /// <summary>
    /// Represents a role in the RBAC system
    /// </summary>
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }  // System roles cannot be deleted
        
        // Navigation property for many-to-many relationship
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
