using System;
using System.Runtime.Serialization;

namespace AppCore.Entities
{
    /// <summary>
    /// Junction table for User-Role many-to-many relationship
    /// </summary>
    [DataContract]
    public class UserRole : BaseEntity
    {
        [DataMember]
        public string UserId { get; set; } = string.Empty;
        [DataMember]
        public string RoleId { get; set; } = string.Empty;
        
    }
}
