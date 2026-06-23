using System.Text;

namespace JiApp.YtDownloader.Features.Assistant;

/// <summary>
/// Per-turn stateful sanitizer that detects tool-call markup leaking into
/// text content and suppresses it before it reaches SSE text-delta events.
/// One instance per <see cref="AssistantChatOrchestrator.StreamAsync"/> turn.
/// </summary>
public sealed class AssistantTextSanitizer
{
    private static readonly string[] Markers =
    [
        "<|tool", "<｜tool", "<invoke", "<|invoke", "<｜invoke",
        "tool_calls", "tool▁calls", "<|parameter", "<parameter name=", "<｜parameter"
    ];

    private static readonly int LongestMarkerLength = Markers.Max(static m => m.Length);

    private readonly StringBuilder _buffer = new();
    private bool _tripped;

    /// <summary>
    /// Processes a text delta. Returns the safe portion to emit, or null
    /// if nothing should be emitted (all buffered or tripped).
    /// </summary>
    public string? ProcessDelta(string delta)
    {
        if (_tripped)
            return null;

        _buffer.Append(delta);
        var accumulated = _buffer.ToString();

        var markerIndex = FindMarkerIndex(accumulated);
        if (markerIndex >= 0)
        {
            _tripped = true;
            return markerIndex > 0 ? accumulated[..markerIndex] : null;
        }

        var safeLength = SafeEmitLength(accumulated);
        if (safeLength <= 0)
            return null;

        var safe = accumulated[..safeLength];
        _buffer.Clear();
        if (safeLength < accumulated.Length)
            _buffer.Append(accumulated[safeLength..]);
        return safe;
    }

    private static int FindMarkerIndex(string text)
    {
        var minIndex = int.MaxValue;
        foreach (var marker in Markers)
        {
            var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && idx < minIndex)
                minIndex = idx;
        }
        return minIndex == int.MaxValue ? -1 : minIndex;
    }

    private static int SafeEmitLength(string text)
    {
        var maxSuffix = Math.Min(LongestMarkerLength - 1, text.Length);
        for (var suffixLen = maxSuffix; suffixLen > 0; suffixLen--)
        {
            var suffix = text[^suffixLen..];
            if (IsMarkerPrefix(suffix))
                return text.Length - suffixLen;
        }
        return text.Length;
    }

    private static bool IsMarkerPrefix(string suffix)
    {
        foreach (var marker in Markers)
        {
            if (marker.Length >= suffix.Length
                && marker.AsSpan(0, suffix.Length).Equals(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
