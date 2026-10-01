using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteDragon
{
    /// <summary>Id and file-name helpers for authoring tools. No UnityEditor dependency, so it is unit tested.</summary>
    public static class ContentIds
    {
        /// <summary>"Iron Tooth", "IronTooth", "Giant's Gut!" become "iron_tooth", "iron_tooth", "giants_gut".</summary>
        public static string ToSnakeCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var sb = new StringBuilder();
            char prev = '\0';
            foreach (char c in name.Trim())
            {
                if (c == '\'' || c == '’') continue;
                if (char.IsLetterOrDigit(c))
                {
                    if (char.IsUpper(c) && (char.IsLower(prev) || char.IsDigit(prev))) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                {
                    sb.Append('_');
                }
                prev = c;
            }
            return sb.ToString().Trim('_');
        }

        /// <summary>baseId if free, otherwise baseId_2, baseId_3... Comparison ignores case.</summary>
        public static string MakeUnique(string baseId, IEnumerable<string> taken)
        {
            var used = new HashSet<string>(taken ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (!used.Contains(baseId)) return baseId;
            for (int n = 2; ; n++)
            {
                string candidate = $"{baseId}_{n}";
                if (!used.Contains(candidate)) return candidate;
            }
        }

        /// <summary>The typed name with characters that are invalid in file names removed.</summary>
        public static string ToFileName(string name, string fallback = "New Asset")
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;
            var invalid = Path.GetInvalidFileNameChars();
            string clean = new string(name.Trim().Where(c => Array.IndexOf(invalid, c) < 0).ToArray()).Trim();
            return clean.Length == 0 ? fallback : clean;
        }
    }
}
