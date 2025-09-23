using Microsoft.EntityFrameworkCore;
using Vira.Domain.Entities;

namespace Vira.Infrastructure;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Vira.Domain.Entities.Category> Categories => Set<Vira.Domain.Entities.Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Request> Requests => Set<Request>();

    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();

    public object RequestComments { get; internal set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vira.Domain.Entities.Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).IsRequired().HasMaxLength(200);
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.Role).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.Token }).IsUnique();
            b.Property(x => x.Token).IsRequired().HasMaxLength(200);
            b.HasOne<User>()
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Request>(e =>
        {
            e.ToTable("requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);

            e.Property(x => x.CategoryId).IsRequired();
            e.Property(x => x.Latitude).IsRequired();
            e.Property(x => x.Longitude).IsRequired();

            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.Status, x.CategoryId });
            e.HasIndex(x => x.CreatedByUserId);

            e.Property(x => x.Location)
                .HasColumnType("geography (Point,4326)"); // metre ile mesafe ölçebilmek için geography
            e.HasIndex(x => x.Location).HasMethod("GIST"); // spatial index

            // kategori ilişkisini “sadece id” ile tutuyoruz; ileride Category navigation eklenebilir.
        });

        modelBuilder.Entity<RequestAttachment>(e =>
        {
            e.ToTable("request_attachments");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(260);
            e.Property(x => x.OriginalName).IsRequired().HasMaxLength(260);
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            e.Property(x => x.SizeBytes).IsRequired();
            e.Property(x => x.Url).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.RequestId);
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestComment>(e =>
        {
            e.ToTable("request_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.RequestId).IsRequired();
            e.Property(x => x.AuthorUserId).IsRequired();
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Type).HasConversion<int>().IsRequired();
            e.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

            e.HasIndex(x => x.RequestId);
            e.HasIndex(x => new { x.RequestId, x.IsDeleted });

            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        base.OnModelCreating(modelBuilder);
    }

}
