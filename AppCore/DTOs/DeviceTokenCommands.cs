namespace AppCore.DTOs;

public class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // "web" (chrome, firefox, safari, edge)
    public string? DeviceName { get; set; }
}

public class SubscribeToSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class UnsubscribeFromSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
}
