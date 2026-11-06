using Microsoft.EntityFrameworkCore;

namespace DL_ALL_IN_ONE;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AppListing> Apps => Set<AppListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Role).HasMaxLength(30).HasDefaultValue("User").IsRequired();
        });
        modelBuilder.Entity<AppListing>(e =>
        {
            e.ToTable("Apps");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.Property(x => x.Tagline).HasMaxLength(180).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.WebsiteUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.LogoUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.Platforms).HasMaxLength(200).IsRequired();
            e.Property(x => x.Pricing).HasMaxLength(120).IsRequired();
            e.Property(x => x.FeaturesJson).HasMaxLength(3000).IsRequired();
            e.Property(x => x.Rating).HasPrecision(2, 1);
        });
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.User).WithMany(x => x.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public interface IUserRepository
{
    Task<AppUser?> FindByNameAsync(string userName, CancellationToken ct);
    Task AddAsync(AppUser user, CancellationToken ct);
}
public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(string userName, CancellationToken ct);
    Task<Notification?> FindForUserAsync(string userName, int id, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
public interface IAppRepository
{
    Task<IReadOnlyList<AppListing>> GetActiveAsync(CancellationToken ct);
    Task<IReadOnlyList<AppListing>> GetAllAsync(CancellationToken ct);
    Task<AppListing?> FindBySlugAsync(string slug, CancellationToken ct);
    Task<AppListing?> FindByIdAsync(int id, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task AddAsync(AppListing app, CancellationToken ct);
    void Remove(AppListing app);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> FindByNameAsync(string userName, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.UserName == userName, ct);
    public async Task AddAsync(AppUser user, CancellationToken ct) { db.Users.Add(user); await db.SaveChangesAsync(ct); }
}

public sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(string userName, CancellationToken ct) =>
        await db.Notifications.AsNoTracking().Where(x => x.User.UserName == userName).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public Task<Notification?> FindForUserAsync(string userName, int id, CancellationToken ct) =>
        db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.User.UserName == userName, ct);
    public async Task AddAsync(Notification notification, CancellationToken ct) => await db.Notifications.AddAsync(notification, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class AppRepository(AppDbContext db) : IAppRepository
{
    public async Task<IReadOnlyList<AppListing>> GetActiveAsync(CancellationToken ct) => await db.Apps.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync(ct);
    public async Task<IReadOnlyList<AppListing>> GetAllAsync(CancellationToken ct) => await db.Apps.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
    public Task<AppListing?> FindBySlugAsync(string slug, CancellationToken ct) => db.Apps.AsNoTracking().SingleOrDefaultAsync(a => a.Slug == slug && a.IsActive, ct);
    public Task<AppListing?> FindByIdAsync(int id, CancellationToken ct) => db.Apps.SingleOrDefaultAsync(a => a.Id == id, ct);
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) => db.Apps.AnyAsync(a => a.Slug == slug, ct);
    public async Task AddAsync(AppListing app, CancellationToken ct) => await db.Apps.AddAsync(app, ct);
    public void Remove(AppListing app) => db.Apps.Remove(app);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
