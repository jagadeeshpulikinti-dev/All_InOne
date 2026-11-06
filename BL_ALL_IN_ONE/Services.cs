using System.Security.Cryptography;
using DL_ALL_IN_ONE;
using UL_ALL_IN_ONE;

namespace BL_ALL_IN_ONE;

public sealed class AuthService(IUserRepository users, ITokenIssuer tokens) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByNameAsync(Normalize(request.UserName), ct);
        if (user is null || !PasswordSecurity.Verify(request.Password, user.PasswordHash)) return null;
        return tokens.Issue(user.UserName, user.Role);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var name = Normalize(request.UserName);
        if (name.Length is < 3 or > 100) throw new ArgumentException("User name must be 3 to 100 characters.");
        if (request.Password.Length < 12) throw new ArgumentException("Password must be at least 12 characters.");
        if (await users.FindByNameAsync(name, ct) is not null) throw new InvalidOperationException("User name is already registered.");
        await users.AddAsync(new AppUser { UserName = name, PasswordHash = PasswordSecurity.Hash(request.Password), Role = "User", CreatedAtUtc = DateTime.UtcNow }, ct);
        return tokens.Issue(name, "User");
    }
    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}

public static class PasswordSecurity
{
    private const int Iterations = 210_000;
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password, string encoded)
    {
        try
        {
            var parts = encoded.Split('.');
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[1]), int.Parse(parts[0]), HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or ArgumentException or OverflowException) { return false; }
    }
}

public sealed class AppCatalogService(IAppRepository repository) : IAppCatalog
{
    public async Task<IReadOnlyList<AppInfo>> GetPublicAsync(CancellationToken ct = default) => (await repository.GetActiveAsync(ct)).Select(Map).ToArray();
    public async Task<AppInfo?> GetBySlugAsync(string slug, CancellationToken ct = default) => await repository.FindBySlugAsync(slug.Trim().ToLowerInvariant(), ct) is { } app ? Map(app) : null;
    public async Task<IReadOnlyList<AppInfo>> GetAdminListAsync(CancellationToken ct = default) => (await repository.GetAllAsync(ct)).Select(Map).ToArray();

    public async Task<AppInfo> CreateAsync(AppUpsertRequest request, CancellationToken ct = default)
    {
        Validate(request);
        var slug = Slug(request.Name);
        if (await repository.SlugExistsAsync(slug, ct)) throw new InvalidOperationException("An app with this name already exists.");
        var now = DateTime.UtcNow;
        var app = Apply(new AppListing { Slug = slug, CreatedAtUtc = now }, request);
        await repository.AddAsync(app, ct);
        await repository.SaveChangesAsync(ct);
        return Map(app);
    }

    public async Task<AppInfo?> UpdateAsync(int id, AppUpsertRequest request, CancellationToken ct = default)
    {
        Validate(request);
        var app = await repository.FindByIdAsync(id, ct);
        if (app is null) return null;
        Apply(app, request);
        await repository.SaveChangesAsync(ct);
        return Map(app);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var app = await repository.FindByIdAsync(id, ct);
        if (app is null) return false;
        app.IsActive = false;
        app.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private static AppListing Apply(AppListing app, AppUpsertRequest input)
    {
        app.Name = input.Name.Trim(); app.Category = input.Category.Trim(); app.Tagline = input.Tagline.Trim(); app.Description = input.Description.Trim();
        app.WebsiteUrl = input.WebsiteUrl.Trim(); app.LogoUrl = input.LogoUrl.Trim(); app.Platforms = input.Platforms.Trim(); app.Pricing = input.Pricing.Trim();
        app.FeaturesJson = System.Text.Json.JsonSerializer.Serialize(input.Features.Select(x => x.Trim()).Where(x => x.Length > 0).Take(12));
        app.Rating = input.Rating; app.IsActive = input.IsActive; app.UpdatedAtUtc = DateTime.UtcNow;
        return app;
    }

    private static void Validate(AppUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120) throw new ArgumentException("App name is required and must be under 120 characters.");
        if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Trim().Length > 80) throw new ArgumentException("Category is required and must be under 80 characters.");
        if (string.IsNullOrWhiteSpace(request.Tagline) || request.Tagline.Trim().Length > 180) throw new ArgumentException("Tagline is required and must be under 180 characters.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000) throw new ArgumentException("Description is required and must be under 2000 characters.");
        if (!Uri.TryCreate(request.WebsiteUrl, UriKind.Absolute, out var website) || website.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("Website URL must be a valid HTTPS URL.");
        if (!string.IsNullOrWhiteSpace(request.LogoUrl) && !(request.LogoUrl.StartsWith("/", StringComparison.Ordinal) || Uri.TryCreate(request.LogoUrl, UriKind.Absolute, out var logo) && logo.Scheme == Uri.UriSchemeHttps)) throw new ArgumentException("Logo URL must be a local path or HTTPS URL.");
        if (request.Rating is < 0 or > 5) throw new ArgumentException("Rating must be between 0 and 5, or left empty.");
    }

    private static string Slug(string name)
    {
        var value = System.Text.RegularExpressions.Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return value.Length > 90 ? value[..90].TrimEnd('-') : value;
    }
    private static AppInfo Map(AppListing a) => new(a.Id, a.Slug, a.Name, a.Category, a.Tagline, a.Description, a.WebsiteUrl, a.LogoUrl, a.Platforms, a.Pricing, System.Text.Json.JsonSerializer.Deserialize<string[]>(a.FeaturesJson) ?? [], a.Rating, a.IsActive);
}

public sealed class NotificationService(INotificationRepository repository, IUserRepository users) : INotificationService
{
    public async Task<IReadOnlyList<UserNotification>> GetAsync(string userName, CancellationToken ct = default) =>
        (await repository.GetForUserAsync(userName, ct)).Select(Map).ToArray();

    public async Task<UserNotification> CreateAsync(string userName, string message, CancellationToken ct = default)
    {
        var trimmed = message.Trim();
        if (trimmed.Length is < 1 or > 1000) throw new ArgumentException("Notification message must be 1 to 1000 characters.");
        var user = await users.FindByNameAsync(userName, ct) ?? throw new InvalidOperationException("User not found.");
        var entity = new Notification { Message = trimmed, CreatedAtUtc = DateTime.UtcNow, UserId = user.Id };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }
    public async Task MarkReadAsync(string userName, string notificationId, CancellationToken ct = default)
    {
        if (!int.TryParse(notificationId, out var id)) throw new ArgumentException("Invalid notification id.");
        var notification = await repository.FindForUserAsync(userName, id, ct) ?? throw new KeyNotFoundException("Notification not found.");
        notification.IsRead = true;
        await repository.SaveChangesAsync(ct);
    }
    private static UserNotification Map(Notification n) => new(n.Id.ToString(), n.Message, n.CreatedAtUtc, n.IsRead);
}
