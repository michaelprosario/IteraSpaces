using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AppCore.Entities
{
    /// <summary>
    /// Represents a role in the RBAC system
    /// </summary>
    [DataContract]
    public class Role : BaseEntity
    {
        [DataMember] public string Name { get; set; } = string.Empty;
        [DataMember] public string Description { get; set; } = string.Empty;
        [DataMember] public bool IsSystemRole { get; set; }  // System roles cannot be deleted
    }
}
