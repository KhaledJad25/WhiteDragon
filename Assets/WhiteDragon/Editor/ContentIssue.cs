using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One validation finding. Code is a stable short key (used by tests); Asset is what to select.</summary>
    public class ContentIssue
    {
        public readonly IssueSeverity Severity;
        public readonly string Code;
        public readonly string Message;
        public readonly Object Asset;

        public ContentIssue(IssueSeverity severity, string code, string message, Object asset)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Asset = asset;
        }

        public override string ToString() => $"{Severity}: {Message}";
    }
}
