using System.Collections;
using System.Text;

namespace HVTravel.Domain.Utils;

public static class TextEncodingRepair
{
    private static readonly string[] CorruptionMarkers =
    {
        "Ã",
        "Â",
        "Ä",
        "Æ",
        "áº",
        "á»",
        "â€™",
        "â€œ",
        "â€",
        "�"
    };

    private static readonly Encoding Windows1252;
    private static readonly Encoding IsoLatin1;
    private const string VietnameseCharacters = "ăâđêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵĂÂĐÊÔƠƯÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ";

    static TextEncodingRepair()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Windows1252 = Encoding.GetEncoding(1252);
        IsoLatin1 = Encoding.GetEncoding("ISO-8859-1");
    }

    public static string NormalizeText(string? value, string? replacementFallback = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (!LooksCorrupted(value) && string.IsNullOrWhiteSpace(replacementFallback))
        {
            return value;
        }

        var best = value;
        var bestScore = ScoreCandidate(value);

        foreach (var candidate in GetCandidates(value, replacementFallback))
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (!LooksCorrupted(candidate) && LooksCorrupted(best))
            {
                best = candidate;
                bestScore = ScoreCandidate(candidate);
                continue;
            }

            var score = ScoreCandidate(candidate);
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    public static bool LooksCorrupted(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return CorruptionMarkers.Any(marker => value.Contains(marker, StringComparison.Ordinal))
            || value.Any(character => char.IsControl(character) && !char.IsWhiteSpace(character));
    }

    public static bool NormalizeObjectGraph(object? root)
    {
        if (root == null || root is string)
        {
            return false;
        }

        return NormalizeObjectGraph(root, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static IEnumerable<string> GetCandidates(string value, string? replacementFallback)
    {
        if (!string.IsNullOrWhiteSpace(replacementFallback))
        {
            yield return replacementFallback;
        }

        var repairedFromWindows1252 = TryRepair(value, Windows1252);
        if (!string.IsNullOrWhiteSpace(repairedFromWindows1252))
        {
            yield return repairedFromWindows1252;
        }

        var repairedFromLatin1 = TryRepair(value, IsoLatin1);
        if (!string.IsNullOrWhiteSpace(repairedFromLatin1)
            && !string.Equals(repairedFromLatin1, repairedFromWindows1252, StringComparison.Ordinal))
        {
            yield return repairedFromLatin1;
        }
    }

    private static string? TryRepair(string value, Encoding sourceEncoding)
    {
        try
        {
            return Encoding.UTF8.GetString(sourceEncoding.GetBytes(value));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool NormalizeObjectGraph(object current, HashSet<object> visited)
    {
        if (!current.GetType().IsValueType && !visited.Add(current))
        {
            return false;
        }

        if (current is IList list)
        {
            return NormalizeList(list, visited);
        }

        if (IsSimpleType(current.GetType()))
        {
            return false;
        }

        var changed = false;
        foreach (var property in current.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(current);
            if (value is string stringValue)
            {
                var normalized = NormalizeText(stringValue);
                if (!string.Equals(normalized, stringValue, StringComparison.Ordinal))
                {
                    property.SetValue(current, normalized);
                    changed = true;
                }

                continue;
            }

            if (value is IList childList)
            {
                changed |= NormalizeList(childList, visited);
                continue;
            }

            if (value != null && !IsSimpleType(value.GetType()))
            {
                changed |= NormalizeObjectGraph(value, visited);
            }
        }

        return changed;
    }

    private static bool NormalizeList(IList list, HashSet<object> visited)
    {
        var changed = false;
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (item is string stringItem)
            {
                var normalized = NormalizeText(stringItem);
                if (!string.Equals(normalized, stringItem, StringComparison.Ordinal))
                {
                    list[i] = normalized;
                    changed = true;
                }

                continue;
            }

            if (item != null && !IsSimpleType(item.GetType()))
            {
                changed |= NormalizeObjectGraph(item, visited);
            }
        }

        return changed;
    }

    private static bool IsSimpleType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid);
    }

    private static int ScoreCandidate(string value)
    {
        var suspiciousPenalty = CorruptionMarkers.Sum(marker => CountOccurrences(value, marker) * 12);
        var replacementPenalty = value.Count(character => character == '�') * 25;
        var controlPenalty = value.Count(character => char.IsControl(character) && !char.IsWhiteSpace(character)) * 20;
        var vietnameseBonus = value.Count(character => VietnameseCharacters.Contains(character)) * 2;
        var cleanBonus = LooksCorrupted(value) ? 0 : 20;
        return cleanBonus + vietnameseBonus - suspiciousPenalty - replacementPenalty - controlPenalty;
    }

    private static int CountOccurrences(string value, string marker)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }

        return count;
    }
}
