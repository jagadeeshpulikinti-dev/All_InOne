using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ALLINONE_Client.Services;

public sealed class TokenStore
{
    public string? AccessToken { get; private set; }
    public string? UserName { get; private set; }
    public string? Role { get; private set; }
    public void Set(string token, string name, string role) { AccessToken = token; UserName = name; Role = role; }
    public void Clear() { AccessToken = null; UserName = null; Role = null; }
}

public sealed class JwtAuthenticationStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (tokens.AccessToken is not { } token) return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (json.RootElement.TryGetProperty("exp", out var exp) && DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow)
            { tokens.Clear(); return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))); }
        }
        catch { tokens.Clear(); return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))); }
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, tokens.UserName ?? ""), new Claim(ClaimTypes.Role, tokens.Role ?? "User") }, "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}

public sealed class BearerTokenHandler(TokenStore tokens) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (tokens.AccessToken is { } token) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, cancellationToken);
    }
}

public sealed class LoginRedirect(NavigationManager navigation) : ComponentBase
{
    protected override void OnInitialized() => navigation.NavigateTo("/login");
}
