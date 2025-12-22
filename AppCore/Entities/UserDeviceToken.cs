namespace AppCore.Entities;

/// <summary>
/// Stores FCM device tokens for users to enable web push notifications
/// </summary>
public class UserDeviceToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // "web" (browser type: chrome, firefox, safari, edge)
    public string? DeviceName { get; set; }
    public DateTime LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
