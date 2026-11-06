using System.Text;
using BL_ALL_IN_ONE;
using DL_ALL_IN_ONE;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UL_ALL_IN_ONE;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey))  
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Set Jwt:SigningKey using user secrets or the Jwt__SigningKey environment variable.");

    // Local-only fallback so a fresh checkout can run without committing a signing secret.
    // This key changes on every API restart; configure user secrets for a stable local key.
    jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
    builder.Configuration["Jwt:SigningKey"] = jwtKey;
    Console.WriteLine("Warning: using a temporary development JWT key. Set Jwt:SigningKey in user secrets for a stable key.");
}
if (Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAppRepository, AppRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAppCatalog, AppCatalogService>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("BlazorClient", policy => policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>()).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// For a first local run this creates the database/schema. Use EF migrations for subsequent schema changes.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    // EnsureCreated does not evolve a database created by the previous version.
    // Keep the local bootstrap additive and safe for the existing Users/Notifications schema.
    await db.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH(N'dbo.Users', N'Role') IS NULL
            ALTER TABLE [dbo].[Users] ADD [Role] nvarchar(30) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT N'User';
        IF OBJECT_ID(N'dbo.Apps', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Apps] (
                [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Apps] PRIMARY KEY,
                [Slug] nvarchar(100) NOT NULL,
                [Name] nvarchar(120) NOT NULL,
                [Category] nvarchar(80) NOT NULL,
                [Tagline] nvarchar(180) NOT NULL,
                [Description] nvarchar(2000) NOT NULL,
                [WebsiteUrl] nvarchar(500) NOT NULL,
                [LogoUrl] nvarchar(500) NOT NULL,
                [Platforms] nvarchar(200) NOT NULL,
                [Pricing] nvarchar(120) NOT NULL,
                [FeaturesJson] nvarchar(3000) NOT NULL,
                [Rating] decimal(2,1) NULL,
                [IsActive] bit NOT NULL,
                [CreatedAtUtc] datetime2 NOT NULL,
                [UpdatedAtUtc] datetime2 NOT NULL
            );
            CREATE UNIQUE INDEX [IX_Apps_Slug] ON [dbo].[Apps] ([Slug]);
        END;
        """);

    if (!await db.Apps.AnyAsync())
    {
        var now = DateTime.UtcNow;
        db.Apps.AddRange(
            SeedApp("zomato", "Zomato", "Food & Dining", "Discover food and dining options", "Browse restaurants, menus and dining information. Starter listing: verify details before publishing.", "https://www.zomato.com", "Restaurant discovery, menus, dining", now),
            SeedApp("swiggy", "Swiggy", "Food & Dining", "Food and grocery delivery", "Explore food and convenience services from Swiggy. Starter listing: verify details before publishing.", "https://www.swiggy.com", "Food delivery, local services", now),
            SeedApp("blinkit", "Blinkit", "Groceries", "Groceries and everyday essentials", "Explore quick-commerce grocery services. Availability and delivery areas vary; verify details before publishing.", "https://blinkit.com", "Groceries, daily essentials", now),
            SeedApp("amazon", "Amazon", "Shopping", "Online shopping marketplace", "Browse products and shopping services on Amazon. Catalog and availability vary by region.", "https://www.amazon.in", "Online marketplace, product discovery", now),
            SeedApp("flipkart", "Flipkart", "Shopping", "Online shopping marketplace", "Browse products and shopping services on Flipkart. Catalog and availability vary by region.", "https://www.flipkart.com", "Online marketplace, product discovery", now),
            SeedApp("netflix", "Netflix", "Entertainment", "Movies and series streaming", "Explore entertainment titles and streaming plans. Availability varies by region.", "https://www.netflix.com", "Streaming entertainment, watchlists", now));
        await db.SaveChangesAsync();
    }

    var adminName = builder.Configuration["AdminBootstrap:UserName"]?.Trim().ToLowerInvariant();
    var adminPassword = builder.Configuration["AdminBootstrap:Password"];
    if (!string.IsNullOrWhiteSpace(adminName) && !string.IsNullOrWhiteSpace(adminPassword) && adminPassword.Length >= 12)
    {
        var admin = await db.Users.SingleOrDefaultAsync(x => x.UserName == adminName);
        if (admin is null)
            db.Users.Add(new AppUser { UserName = adminName, PasswordHash = PasswordSecurity.Hash(adminPassword), Role = "Admin", CreatedAtUtc = DateTime.UtcNow });
        else if (admin.Role != "Admin") admin.Role = "Admin";
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static AppListing SeedApp(string slug, string name, string category, string tagline, string description, string website, string features, DateTime now) => new()
{
    Slug = slug, Name = name, Category = category, Tagline = tagline, Description = description,
    WebsiteUrl = website, LogoUrl = "", Platforms = "Web, Android, iOS", Pricing = "See provider website",
    FeaturesJson = System.Text.Json.JsonSerializer.Serialize(features.Split(", ")),
    Rating = null, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now
};
