using System.Runtime.Serialization;

namespace AppCore.Entities;

/// <summary>
/// Stores FCM device tokens for users to enable web push notifications
/// </summary>
[DataContract]
public class UserDeviceToken : BaseEntity
{
    [DataMember]
    public string UserId { get; set; } = string.Empty;
    [DataMember]
    public string DeviceToken { get; set; } = string.Empty;
    [DataMember]
    public string DeviceType { get; set; } = string.Empty; // "web" (browser type: chrome, firefox, safari, edge)
    [DataMember]
    public string? DeviceName { get; set; }
    [DataMember]
    public DateTime LastUsedAt { get; set; }
    [DataMember]
    public bool IsActive { get; set; } = true;
}
