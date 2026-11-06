namespace UL_ALL_IN_ONE;

public sealed class LoginRequest { public string UserName { get; set; } = ""; public string Password { get; set; } = ""; }
public sealed class RegisterRequest { public string UserName { get; set; } = ""; public string Password { get; set; } = ""; }
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, string UserName, string Role);
public sealed record AppInfo(int Id, string Slug, string Name, string Category, string Tagline, string Description, string WebsiteUrl, string LogoUrl, string Platforms, string Pricing, string[] Features, decimal? Rating, bool IsActive);
public sealed class AppUpsertRequest
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string Platforms { get; set; } = "";
    public string Pricing { get; set; } = "";
    public string[] Features { get; set; } = [];
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed record ApiMessage(string Message);
public sealed record UserNotification(string Id, string Message, DateTime CreatedAtUtc, bool IsRead);
public sealed class NotificationCreateRequest { public string Message { get; set; } = ""; }
public interface IAuthService { Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default); }
public interface ITokenIssuer { AuthResponse Issue(string userName, string role); }
public interface IAppCatalog
{
    Task<IReadOnlyList<AppInfo>> GetPublicAsync(CancellationToken ct = default);
    Task<AppInfo?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<AppInfo>> GetAdminListAsync(CancellationToken ct = default);
    Task<AppInfo> CreateAsync(AppUpsertRequest request, CancellationToken ct = default);
    Task<AppInfo?> UpdateAsync(int id, AppUpsertRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
public interface INotificationService { Task<IReadOnlyList<UserNotification>> GetAsync(string userName, CancellationToken ct = default); Task<UserNotification> CreateAsync(string userName, string message, CancellationToken ct = default); Task MarkReadAsync(string userName, string notificationId, CancellationToken ct = default); }
