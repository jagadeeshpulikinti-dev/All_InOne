using System.Net.Http.Json;
using UL_ALL_IN_ONE;

namespace ALLINONE_Client.Services;

public sealed class ApiClient(HttpClient http)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) => await PostAsync<AuthResponse>("api/auth/login", request, ct);
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) => await PostAsync<AuthResponse>("api/auth/register", request, ct);
    public async Task<IReadOnlyList<AppInfo>> GetAppsAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<AppInfo[]>("api/apps", ct) ?? Array.Empty<AppInfo>();
    public async Task<AppInfo?> GetAppAsync(string slug, CancellationToken ct = default) => await http.GetFromJsonAsync<AppInfo>($"api/apps/{Uri.EscapeDataString(slug)}", ct);
    public async Task<AuthResponse> AdminLoginAsync(LoginRequest request, CancellationToken ct = default) => await PostAsync<AuthResponse>("api/admin/login", request, ct);
    public async Task<IReadOnlyList<AppInfo>> GetAdminAppsAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<AppInfo[]>("api/admin/apps", ct) ?? Array.Empty<AppInfo>();
    public async Task<AppInfo> CreateAppAsync(AppUpsertRequest request, CancellationToken ct = default) => await PostAsync<AppInfo>("api/admin/apps", request, ct);
    public async Task<AppInfo> UpdateAppAsync(int id, AppUpsertRequest request, CancellationToken ct = default)
    {
        using var response = await http.PutAsJsonAsync($"api/admin/apps/{id}", request, ct); await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<AppInfo>(cancellationToken: ct) ?? throw new InvalidOperationException("The API returned an empty response.");
    }
    public async Task DeleteAppAsync(int id, CancellationToken ct = default) { using var response = await http.DeleteAsync($"api/admin/apps/{id}", ct); await EnsureSuccessAsync(response); }
    public async Task<IReadOnlyList<UserNotification>> GetNotificationsAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<UserNotification[]>("api/notifications", ct) ?? Array.Empty<UserNotification>();
    public async Task CreateNotificationAsync(string message, CancellationToken ct = default) => await PostAsync<UserNotification>("api/notifications", new NotificationCreateRequest { Message = message }, ct);
    public async Task MarkReadAsync(string id, CancellationToken ct = default) { using var response = await http.PutAsync($"api/notifications/{Uri.EscapeDataString(id)}/read", null, ct); await EnsureSuccessAsync(response); }
    private async Task<T> PostAsync<T>(string url, object value, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(url, value, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct) ?? throw new InvalidOperationException("The API returned an empty response.");
    }
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        try { using var doc = System.Text.Json.JsonDocument.Parse(body); if (doc.RootElement.TryGetProperty("message", out var message)) throw new InvalidOperationException(message.GetString() ?? response.ReasonPhrase); } catch (System.Text.Json.JsonException) { }
        throw new InvalidOperationException($"API error {(int)response.StatusCode}: {response.ReasonPhrase}");
    }
}
