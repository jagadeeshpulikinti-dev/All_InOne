namespace DL_ALL_IN_ONE;

public sealed class AppUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public sealed class AppListing
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string Platforms { get; set; } = "";
    public string Pricing { get; set; } = "";
    public string FeaturesJson { get; set; } = "[]";
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class Notification
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRead { get; set; }
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
