using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MoonTweaks.DocGen;

/// <summary>
/// Colours a snippet of Lua for the reference page.
/// </summary>
/// <remarks>
/// Hand-rolled rather than fetched. Lua's lexical grammar is four things — comments,
/// strings, numbers, and a closed list of keywords — which is small enough to read in
/// one pass, and the alternative is a page that cannot show its own examples until a
/// content delivery network answers. The reference stays one file that highlights
/// itself with nothing loaded and nothing running.
/// </remarks>
public static class LuaHighlighter
{
    /// <summary>Lua 5.2's reserved words, which is the version the interpreter reports.</summary>
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function",
        "if", "in", "local", "nil", "not", "or", "repeat", "return", "then",
        "true", "until", "while",
    };

    /// <summary>
    /// Renders code as HTML, escaping as it goes. Escaping happens here rather than
    /// before or after, because a marked-up string and an escaped one cannot be told
    /// apart once either has been done to the other.
    /// </summary>
    public static string Highlight(string code)
    {
        var html = new StringBuilder();
        var at = 0;

        while (at < code.Length)
        {
            var start = at;
            var bracket = LongBracketLevel(code, at);

            if (code.AsSpan(at).StartsWith("--", StringComparison.Ordinal))
            {
                // A comment is long or short by whatever follows the two dashes.
                var level = LongBracketLevel(code, at + 2);
                at = level >= 0 ? EndOfLong(code, at + 2, level) : EndOfLine(code, at);
                Span(html, "comment", code[start..at]);
            }
            else if (bracket >= 0)
            {
                at = EndOfLong(code, at, bracket);
                Span(html, "string", code[start..at]);
            }
            else if (code[at] is '"' or '\'')
            {
                at = EndOfQuoted(code, at);
                Span(html, "string", code[start..at]);
            }
            else if (char.IsAsciiDigit(code[at]))
            {
                // Letters belong to a number here so that 0x1f and 1e3 stay whole.
                while (at < code.Length && (char.IsLetterOrDigit(code[at]) || code[at] == '.')) at++;
                Span(html, "number", code[start..at]);
            }
            else if (char.IsLetter(code[at]) || code[at] == '_')
            {
                while (at < code.Length && (char.IsLetterOrDigit(code[at]) || code[at] == '_')) at++;
                var word = code[start..at];

                if (Keywords.Contains(word)) Span(html, "keyword", word);
                else html.Append(WebUtility.HtmlEncode(word));
            }
            else
            {
                at++;
                html.Append(WebUtility.HtmlEncode(code[start..at]));
            }
        }

        return html.ToString();
    }

    /// <summary>
    /// How many <c>=</c> sit inside the long bracket opening here, or -1 where none
    /// opens. The count is what the closing bracket has to match, so it is the whole
    /// of what a caller needs to find the end.
    /// </summary>
    private static int LongBracketLevel(string code, int at)
    {
        if (at >= code.Length || code[at] != '[') return -1;

        var scan = at + 1;
        while (scan < code.Length && code[scan] == '=') scan++;

        return scan < code.Length && code[scan] == '[' ? scan - at - 1 : -1;
    }

    /// <summary>Index past the bracket closing a long string or comment.</summary>
    private static int EndOfLong(string code, int at, int level)
    {
        var closing = $"]{new string('=', level)}]";
        var end = code.IndexOf(closing, at, StringComparison.Ordinal);

        // An unclosed one runs to the end of the snippet rather than throwing: this
        // renders documentation, and half a colour is a better answer than no page.
        return end < 0 ? code.Length : end + closing.Length;
    }

    /// <summary>Index of the line break ending a short comment, or the end of the code.</summary>
    private static int EndOfLine(string code, int at)
    {
        var end = code.IndexOf('\n', at);
        return end < 0 ? code.Length : end;
    }

    /// <summary>Index past the quote closing a string, counting backslash escapes.</summary>
    private static int EndOfQuoted(string code, int at)
    {
        var quote = code[at];
        var scan = at + 1;

        // A short string cannot cross a line, so an unclosed one ends where Lua
        // would end it. The snippet check refuses one long before the page is
        // written; this keeps a typo from colouring everything after it anyway.
        while (scan < code.Length && code[scan] != quote && code[scan] != '\n')
        {
            scan += code[scan] == '\\' ? 2 : 1;
        }

        if (scan < code.Length && code[scan] == '\n') return scan;

        return Math.Min(scan + 1, code.Length);
    }

    private static void Span(StringBuilder html, string kind, string text) =>
        html.Append($"<span class=\"{kind}\">{WebUtility.HtmlEncode(text)}</span>");
}
