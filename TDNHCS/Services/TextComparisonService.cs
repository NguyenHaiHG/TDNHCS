using System.Text.RegularExpressions;
using TDNHCS.Models;

namespace TDNHCS.Services;

/// <summary>
/// So sánh văn bản: Myers diff theo dòng + diff từng từ cho dòng Modified
/// + cosine TF-IDF. Chuẩn hóa Unicode tiếng Việt trước khi so sánh.
/// </summary>
public sealed partial class TextComparisonService
{
    public DocumentComparisonResult Compare(string leftText, string rightText)
    {
        leftText = Normalize(leftText ?? string.Empty);
        rightText = Normalize(rightText ?? string.Empty);

        var leftLines = GetLines(leftText);
        var rightLines = GetLines(rightText);
        var edits = BuildMyersDiff(leftLines, rightLines);
        var rows = BuildRows(edits);

        return new DocumentComparisonResult
        {
            Similarity = CalculateTfIdfSimilarity(leftText, rightText),
            Rows = rows,
            AddedCount = rows.Count(row => row.Kind == DiffKind.Added),
            RemovedCount = rows.Count(row => row.Kind == DiffKind.Removed),
            ModifiedCount = rows.Count(row => row.Kind == DiffKind.Modified)
        };
    }

    /// <summary>Chuẩn hóa: bỏ khoảng trắng thừa, chuẩn hóa NFC Unicode.</summary>
    private static string Normalize(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Normalize(System.Text.NormalizationForm.FormC)
                  .Replace("\r\n", "\n", StringComparison.Ordinal)
                  .Replace('\r', '\n');

    private static string[] GetLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        return text.Split('\n');
    }

    // ── Myers diff ──────────────────────────────────────────────────────────

    private static List<Edit> BuildMyersDiff(string[] left, string[] right)
    {
        var n = left.Length;
        var m = right.Length;
        var max = n + m;
        var frontier = new Dictionary<int, int> { [1] = 0 };
        var trace = new List<Dictionary<int, int>>();

        for (var distance = 0; distance <= max; distance++)
        {
            trace.Add(new Dictionary<int, int>(frontier));

            for (var diagonal = -distance; diagonal <= distance; diagonal += 2)
            {
                int x;
                if (diagonal == -distance ||
                    (diagonal != distance &&
                     Get(frontier, diagonal - 1) < Get(frontier, diagonal + 1)))
                {
                    x = Get(frontier, diagonal + 1);
                }
                else
                {
                    x = Get(frontier, diagonal - 1) + 1;
                }

                var y = x - diagonal;
                while (x < n && y < m &&
                       string.Equals(left[x], right[y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                frontier[diagonal] = x;
                if (x >= n && y >= m)
                {
                    return Backtrack(trace, left, right, distance);
                }
            }
        }

        return [];
    }

    private static List<Edit> Backtrack(
        IReadOnlyList<Dictionary<int, int>> trace,
        string[] left,
        string[] right,
        int finalDistance)
    {
        var edits = new List<Edit>();
        var x = left.Length;
        var y = right.Length;

        for (var distance = finalDistance; distance >= 0; distance--)
        {
            var frontier = trace[distance];
            var diagonal = x - y;
            var previousDiagonal =
                diagonal == -distance ||
                (diagonal != distance &&
                 Get(frontier, diagonal - 1) < Get(frontier, diagonal + 1))
                    ? diagonal + 1
                    : diagonal - 1;

            var previousX = Get(frontier, previousDiagonal);
            var previousY = previousX - previousDiagonal;

            while (x > previousX && y > previousY)
            {
                edits.Add(new Edit(EditKind.Equal, left[x - 1]));
                x--;
                y--;
            }

            if (distance == 0)
            {
                break;
            }

            if (x == previousX)
            {
                edits.Add(new Edit(EditKind.Add, right[y - 1]));
                y--;
            }
            else
            {
                edits.Add(new Edit(EditKind.Remove, left[x - 1]));
                x--;
            }
        }

        edits.Reverse();
        return edits;
    }

    // ── Build DiffRows với word-level diff ──────────────────────────────────

    private static IReadOnlyList<DiffRow> BuildRows(IReadOnlyList<Edit> edits)
    {
        var rows = new List<DiffRow>();
        var leftLine = 1;
        var rightLine = 1;
        var index = 0;

        while (index < edits.Count)
        {
            if (edits[index].Kind == EditKind.Equal)
            {
                rows.Add(new DiffRow
                {
                    LeftLineNumber = leftLine++,
                    LeftText = edits[index].Text,
                    RightLineNumber = rightLine++,
                    RightText = edits[index].Text,
                    Kind = DiffKind.Unchanged
                });
                index++;
                continue;
            }

            var removed = new List<string>();
            var added = new List<string>();
            while (index < edits.Count && edits[index].Kind != EditKind.Equal)
            {
                if (edits[index].Kind == EditKind.Remove)
                    removed.Add(edits[index].Text);
                else
                    added.Add(edits[index].Text);
                index++;
            }

            var pairedCount = Math.Min(removed.Count, added.Count);
            for (var pair = 0; pair < pairedCount; pair++)
            {
                var (leftDiff, rightDiff) = WordDiff(removed[pair], added[pair]);
                rows.Add(new DiffRow
                {
                    LeftLineNumber = leftLine++,
                    LeftText = removed[pair],
                    RightLineNumber = rightLine++,
                    RightText = added[pair],
                    Kind = DiffKind.Modified,
                    LeftWordDiff = leftDiff,
                    RightWordDiff = rightDiff
                });
            }

            for (var remove = pairedCount; remove < removed.Count; remove++)
            {
                rows.Add(new DiffRow
                {
                    LeftLineNumber = leftLine++,
                    LeftText = removed[remove],
                    Kind = DiffKind.Removed
                });
            }

            for (var add = pairedCount; add < added.Count; add++)
            {
                rows.Add(new DiffRow
                {
                    RightLineNumber = rightLine++,
                    RightText = added[add],
                    Kind = DiffKind.Added
                });
            }
        }

        return rows;
    }

    // ── Word-level diff (Myers trên token) ─────────────────────────────────

    private static (IReadOnlyList<WordSpan> left, IReadOnlyList<WordSpan> right)
        WordDiff(string leftLine, string rightLine)
    {
        var lt = TokenizeLine(leftLine);
        var rt = TokenizeLine(rightLine);
        var edits = BuildMyersDiff(lt, rt);

        var leftSpans = new List<WordSpan>();
        var rightSpans = new List<WordSpan>();

        foreach (var edit in edits)
        {
            switch (edit.Kind)
            {
                case EditKind.Equal:
                    leftSpans.Add(new WordSpan { Text = edit.Text, Kind = WordDiffKind.Equal });
                    rightSpans.Add(new WordSpan { Text = edit.Text, Kind = WordDiffKind.Equal });
                    break;
                case EditKind.Remove:
                    leftSpans.Add(new WordSpan { Text = edit.Text, Kind = WordDiffKind.Removed });
                    break;
                case EditKind.Add:
                    rightSpans.Add(new WordSpan { Text = edit.Text, Kind = WordDiffKind.Added });
                    break;
            }
        }

        return (leftSpans, rightSpans);
    }

    private static string[] TokenizeLine(string line) =>
        string.IsNullOrEmpty(line)
            ? []
            : TokenRegex().Matches(line).Select(m => m.Value).ToArray();

    // ── TF-IDF cosine similarity ────────────────────────────────────────────

    private static double CalculateTfIdfSimilarity(string leftText, string rightText)
    {
        if (string.Equals(leftText, rightText, StringComparison.Ordinal))
        {
            return 1;
        }

        var leftTerms = Tokenize(leftText);
        var rightTerms = Tokenize(rightText);
        if (leftTerms.Count == 0 || rightTerms.Count == 0)
        {
            return 0;
        }

        var leftFrequency = leftTerms
            .GroupBy(term => term)
            .ToDictionary(g => g.Key, g => g.Count());
        var rightFrequency = rightTerms
            .GroupBy(term => term)
            .ToDictionary(g => g.Key, g => g.Count());
        var vocabulary = leftFrequency.Keys
            .Union(rightFrequency.Keys, StringComparer.Ordinal)
            .ToArray();

        var dotProduct = 0d;
        var leftMagnitude = 0d;
        var rightMagnitude = 0d;

        foreach (var term in vocabulary)
        {
            var df =
                (leftFrequency.ContainsKey(term) ? 1 : 0) +
                (rightFrequency.ContainsKey(term) ? 1 : 0);
            var idf = Math.Log(3d / (df + 1d)) + 1d;
            var lw = leftFrequency.GetValueOrDefault(term) / (double)leftTerms.Count * idf;
            var rw = rightFrequency.GetValueOrDefault(term) / (double)rightTerms.Count * idf;

            dotProduct += lw * rw;
            leftMagnitude += lw * lw;
            rightMagnitude += rw * rw;
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return Math.Clamp(
            dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)),
            0, 1);
    }

    private static List<string> Tokenize(string text) =>
        WordRegex().Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .ToList();

    private static int Get(IReadOnlyDictionary<int, int> values, int key) =>
        values.TryGetValue(key, out var value) ? value : 0;

    [GeneratedRegex(@"[\p{L}\p{Nd}]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    /// <summary>Token hóa giữ khoảng trắng để word diff có thể render đúng.</summary>
    [GeneratedRegex(@"\S+|\s+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    private enum EditKind { Equal, Add, Remove }
    private sealed record Edit(EditKind Kind, string Text);
}
