using Microsoft.EntityFrameworkCore;
using System.IO;
using TDNHCS.Models;

namespace TDNHCS.Data;

/// <summary>
/// Database Context cho ứng dụng quản lý văn bản
/// </summary>
public class DocumentDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Tạo database trong thư mục AppData
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TDNHCS",
            "documents.db"
        );
        
        // Tạo thư mục nếu chưa có
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình Entity
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

        // Seed dữ liệu mẫu
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Tín Dụng", Description = "Văn bản Tín dụng" },
            new Category { Id = 2, Name = "Kế toán", Description = "Văn bản Kế toán" },
            new Category { Id = 3, Name = "Kiểm tra, kiểm soát", Description = "Văn bản KTKSNB" },
            new Category { Id = 4, Name = "Trả lời", Description = "Văn bản trả lời" }
        );
    }
}
