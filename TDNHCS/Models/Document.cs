using System.ComponentModel.DataAnnotations.Schema;

namespace TDNHCS.Models;

/// <summary>
/// Thực thể văn bản
/// </summary>
public class Document {
    public int Id { get; set; }
    
    /// <summary>
    /// Số văn bản
    /// </summary>
    public string DocumentNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Tiêu đề văn bản
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Nội dung tóm tắt
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Nội dung đầy đủ của văn bản để lưu và tìm kiếm trong SQL
    /// </summary>
    public string? Content { get; set; }
    
    /// <summary>
    /// Ngày ban hành
    /// </summary>
    public DateTime IssueDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Ngày nhận văn bản
    /// </summary>
    public DateTime ReceivedDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Đường dẫn file đính kèm (lưu tên GUID trên disk)
    /// </summary>
    public string? FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Tên file gốc (để hiển thị cho người dùng)
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Loại văn bản
    /// </summary>
    public DocumentType Type { get; set; } = DocumentType.Incoming;
    
    /// <summary>
    /// Danh mục
    /// </summary>
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    
    /// <summary>
    /// Người tạo
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    [NotMapped]
    public string StorageLocation
    {
        get
        {
            var databaseLocation = $"SQL: {AppPaths.DatabasePath}";
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                return $"{databaseLocation} | File đính kèm: chưa có";
            }

            return $"{databaseLocation} | File đính kèm: {FilePath}";
        }
    }
}

/// <summary>
/// Loại văn bản
/// </summary>
public enum DocumentType
{
    Incoming = 0,      // Văn bản đến
    Outgoing = 1,      // Văn bản đi
    Internal = 2       // Văn bản nội bộ
}
