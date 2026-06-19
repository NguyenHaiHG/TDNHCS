using Microsoft.EntityFrameworkCore;
using System.IO;
using TDNHCS.Data;
using TDNHCS.Models;

namespace TDNHCS.Services;

/// <summary>
/// Service quản lý văn bản
/// </summary>
public class DocumentService
{
    private readonly DocumentDbContext _context;
    private readonly DataStoreBootstrap _bootstrap;

    private static readonly List<Category> DefaultCategories =
    [
        new Category { Id = 1, Name = "Tín Dụng", Description = "Văn bản Tín dụng" },
        new Category { Id = 2, Name = "Kế toán", Description = "Văn bản Kế toán" },
        new Category { Id = 3, Name = "Kiểm tra, kiểm soát", Description = "Văn bản KTKSNB" },
        new Category { Id = 4, Name = "Trả lời", Description = "Văn bản trả lời" },
        new Category { Id = 5, Name = "Ngoại ngành", Description = "Văn bản khác" }
    ];

    public DocumentService(DocumentDbContext context, DataStoreBootstrap bootstrap)
    {
        _context = context;
        _bootstrap = bootstrap;
    }

    /// <summary>
    /// Lấy tất cả văn bản
    /// </summary>
    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        if (!_bootstrap.IsInitialized)
        {
            return [];
        }

        return await _context.Documents
            .Include(d => d.Category)
            .OrderByDescending(d => d.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy văn bản theo ID
    /// </summary>
    public async Task<Document?> GetDocumentByIdAsync(int id)
    {
        if (!_bootstrap.IsInitialized)
        {
            return null;
        }

        return await _context.Documents
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// Tìm kiếm văn bản
    /// </summary>
    public async Task<List<Document>> SearchDocumentsAsync(string searchText)
    {
        if (!_bootstrap.IsInitialized)
        {
            return [];
        }

        searchText = searchText.Trim();

        return await _context.Documents
            .Include(d => d.Category)
            .Where(d => d.Title.Contains(searchText) ||
                       d.DocumentNumber.Contains(searchText) ||
                       (d.Summary != null && d.Summary.Contains(searchText)) ||
                       (d.Content != null && d.Content.Contains(searchText)) ||
                       (d.Notes != null && d.Notes.Contains(searchText)))
            .OrderByDescending(d => d.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Thêm văn bản mới — khởi tạo database lần đầu tại đây.
    /// </summary>
    public async Task<Document> AddDocumentAsync(Document document)
    {
        await _bootstrap.EnsureInitializedAsync();
        await StoreAttachmentIfNeededAsync(document);

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    /// <summary>
    /// Cập nhật văn bản
    /// </summary>
    public async Task UpdateDocumentAsync(Document document)
    {
        await _bootstrap.EnsureInitializedAsync();
        await StoreAttachmentIfNeededAsync(document);

        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa văn bản
    /// </summary>
    public async Task DeleteDocumentAsync(int id)
    {
        if (!_bootstrap.IsInitialized)
        {
            return;
        }

        var document = await _context.Documents.FindAsync(id);
        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Lấy tất cả danh mục
    /// </summary>
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        if (!_bootstrap.IsInitialized)
        {
            return DefaultCategories.ToList();
        }

        return await _context.Categories.ToListAsync();
    }

    public async Task MigrateStoredPathsAsync()
    {
        if (!_bootstrap.IsInitialized)
        {
            return;
        }

        var documents = await _context.Documents.ToListAsync();
        var changed = false;

        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                continue;
            }

            var storedPath = AppPaths.ToStoredPath(AppPaths.ResolveStoredPath(document.FilePath));
            if (!storedPath.Equals(document.FilePath, StringComparison.Ordinal))
            {
                document.FilePath = storedPath;
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static async Task StoreAttachmentIfNeededAsync(Document document)
    {
        var sourcePath = AppPaths.ResolveStoredPath(document.FilePath);
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        var attachmentsRoot = Path.GetFullPath(AppPaths.AttachmentsFolder);
        var resolvedPath = Path.GetFullPath(sourcePath);

        if (resolvedPath.StartsWith(attachmentsRoot, StringComparison.OrdinalIgnoreCase))
        {
            document.FilePath = AppPaths.ToStoredPath(resolvedPath);
            return;
        }

        var extension = Path.GetExtension(resolvedPath);
        var storedFileName = Guid.NewGuid().ToString("N") + extension;
        var destinationPath = Path.Combine(attachmentsRoot, storedFileName);

        await Task.Run(() => File.Copy(resolvedPath, destinationPath, true));
        document.FilePath = AppPaths.ToStoredPath(destinationPath);
    }
}
