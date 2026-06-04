using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TDNHCS.Models;

namespace TDNHCS.Data;

/// <summary>
/// Database Context cho ứng dụng quản lý văn bản
/// </summary>
public class DocumentDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        AppPaths.EnsureDirectories();
        optionsBuilder.UseSqlite($"Data Source={AppPaths.DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Seed danh mục
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Tín Dụng",            Description = "Văn bản Tín dụng" },
            new Category { Id = 2, Name = "Kế toán",             Description = "Văn bản Kế toán" },
            new Category { Id = 3, Name = "Kiểm tra, kiểm soát", Description = "Văn bản KTKSNB" },
            new Category { Id = 4, Name = "Trả lời",             Description = "Văn bản trả lời" },
            new Category { Id = 5, Name = "Ngoại ngành",         Description = "Văn bản khác" }
        );

        // Seed tài khoản mặc định: admin / Admin@123
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = HashPassword("Admin@123"),
                FullName = "Quản trị viên",
                Role = "Admin",
                CreatedDate = new DateTime(2024, 1, 1)
            }
        );
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
