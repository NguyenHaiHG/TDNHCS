namespace TDNHCS.Models;

/// <summary>
/// Danh mục văn bản
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    /// <summary>
    /// Tên danh mục
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Danh sách văn bản
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
