using System.Text.RegularExpressions;

namespace TGDev.Step03.Vectorization.Utils;

public static class Ingest
{
    public static IEnumerable<string> Chunk(string text, int maxTokens = 400)
    {
        // Normalize whitespace
        var cleaned = Regex.Replace(text, "\\s+", " ").Trim();

        // Split by sentence
        var sentences = cleaned.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

        var current = new List<string>();

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var candidate = string.Join(" ", current.Append(trimmed));
            var tokenCount = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (tokenCount > maxTokens)
            {
                if (current.Count > 0)
                {
                    yield return string.Join(" ", current);
                }
                current.Clear();
            }

            current.Add(trimmed);
        }

        if (current.Count > 0)
        {
            yield return string.Join(" ", current);
        }
    }
}
