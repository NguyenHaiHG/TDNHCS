using Microsoft.EntityFrameworkCore;
using TDNHCS.Data;
using TDNHCS.Models;

namespace TDNHCS.Services;

/// <summary>
/// Service quản lý văn bản
/// </summary>
public class DocumentService
{
    private readonly DocumentDbContext _context;

    public DocumentService(DocumentDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy tất cả văn bản
    /// </summary>
    public async Task<List<Document>> GetAllDocumentsAsync()
    {
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
        return await _context.Documents
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// Tìm kiếm văn bản
    /// </summary>
    public async Task<List<Document>> SearchDocumentsAsync(string searchText)
    {
        return await _context.Documents
            .Include(d => d.Category)
            .Where(d => d.Title.Contains(searchText) || 
                       d.DocumentNumber.Contains(searchText) ||
                       (d.Summary != null && d.Summary.Contains(searchText)))
            .OrderByDescending(d => d.ReceivedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Thêm văn bản mới
    /// </summary>
    public async Task<Document> AddDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    /// <summary>
    /// Cập nhật văn bản
    /// </summary>
    public async Task UpdateDocumentAsync(Document document)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa văn bản
    /// </summary>
    public async Task DeleteDocumentAsync(int id)
    {
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
        return await _context.Categories.ToListAsync();
    }
}
