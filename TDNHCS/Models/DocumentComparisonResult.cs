namespace TDNHCS.Models;

public enum DiffKind
{
    Unchanged,
    Added,
    Removed,
    Modified
}

public sealed class DiffRow
{
    public int? LeftLineNumber { get; init; }
    public string LeftText { get; init; } = string.Empty;
    public int? RightLineNumber { get; init; }
    public string RightText { get; init; } = string.Empty;
    public DiffKind Kind { get; init; }

    public string ChangeDisplay => Kind switch
    {
        DiffKind.Added => "Thêm",
        DiffKind.Removed => "Xóa",
        DiffKind.Modified => "Thay đổi",
        _ => "Giữ nguyên"
    };
}

public sealed class DocumentComparisonResult
{
    public double Similarity { get; init; }
    public IReadOnlyList<DiffRow> Rows { get; init; } = [];
    public int AddedCount { get; init; }
    public int RemovedCount { get; init; }
    public int ModifiedCount { get; init; }
}
