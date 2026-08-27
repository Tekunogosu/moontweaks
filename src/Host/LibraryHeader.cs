using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MoonTweaks.Host;

/// <summary>
/// The header a generated file carries. The reference generator writes it and
/// <see cref="EditorSupport"/> reads it, which is how a server decides whether the
/// copy already on disk is the one this build would write. Both sides therefore
/// share one description of the format rather than agreeing by coincidence.
/// </summary>
public static class LibraryHeader
{
    /// <summary>Prefix of the line naming the build that produced a file.</summary>
    public const string BuildMarker = "--- build ";

    /// <summary>How far into a file the marker is looked for, bounding the read.</summary>
    public const int Depth = 8;

    /// <summary>Sixteen hex digits of the content's SHA-256, enough to name a build.</summary>
    public static string Fingerprint(string content) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..16];

    /// <summary>The marker these lines carry, or null when they carry none.</summary>
    public static string? MarkerIn(IEnumerable<string> lines) =>
        lines.Take(Depth)
            .Select(line => line.TrimEnd('\r'))
            .FirstOrDefault(line => line.StartsWith(BuildMarker, StringComparison.Ordinal));
}
