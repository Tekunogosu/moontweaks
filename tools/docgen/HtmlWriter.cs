using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MoonTweaks.DocGen;

/// <summary>Renders the API model as a self-contained page suitable for GitHub Pages.</summary>
public static partial class HtmlWriter
{
    /// <summary>Renders the whole reference.</summary>
    public static string Write(ApiModel api)
    {
        var page = new StringBuilder();
        page.AppendLine("<!doctype html>");
        page.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        page.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        page.AppendLine($"<title>MoonTweaks {Escape(api.Version)} API</title>");
        page.AppendLine($"<style>{Stylesheet}</style></head><body>");

        WriteSidebar(page, api);

        page.AppendLine("<main>");
        page.AppendLine($"<h1>MoonTweaks scripting API <span class=\"version\">{Escape(api.Version)}</span></h1>");
        page.AppendLine("<p class=\"lede\">Generated from the mod's bindings. Every function and field listed here "
                        + "is one the interpreter actually exposes.</p>");

        foreach (var module in api.Modules) WriteModule(page, module);
        foreach (var table in api.Tables) WriteTable(page, table);
        foreach (var enumeration in api.Enums) WriteEnum(page, enumeration);

        page.AppendLine("</main></body></html>");
        return page.ToString();
    }

    private static void WriteSidebar(StringBuilder page, ApiModel api)
    {
        page.AppendLine("<nav><div class=\"brand\">MoonTweaks</div>");

        page.AppendLine("<h2>Modules</h2><ul>");
        foreach (var module in api.Modules)
        {
            page.AppendLine($"<li><a href=\"#{Anchor(module.Path)}\"><code>{Escape(module.Path)}</code></a></li>");
        }
        page.AppendLine("</ul>");

        page.AppendLine("<h2>Tables</h2><ul>");
        foreach (var table in api.Tables)
        {
            page.AppendLine($"<li><a href=\"#{Anchor(table.Name)}\">{Escape(table.Name)}</a></li>");
        }
        page.AppendLine("</ul>");

        if (api.Enums.Count > 0)
        {
            page.AppendLine("<h2>Values</h2><ul>");
            foreach (var enumeration in api.Enums)
            {
                page.AppendLine($"<li><a href=\"#{Anchor(enumeration.Name)}\">{Escape(enumeration.Name)}</a></li>");
            }
            page.AppendLine("</ul>");
        }

        page.AppendLine("</nav>");
    }

    private static void WriteModule(StringBuilder page, ModuleDoc module)
    {
        page.AppendLine($"<section id=\"{Anchor(module.Path)}\">");
        page.AppendLine($"<h2 class=\"module\"><code>{Escape(module.Path)}</code></h2>");
        page.AppendLine($"<p>{Markup(module.Summary)}</p>");

        foreach (var function in module.Functions)
        {
            var arguments = string.Join(", ", function.Parameters.Select(p => Escape(p.Name)));
            page.AppendLine($"<div class=\"item\" id=\"{Anchor($"{module.Path}.{function.Name}")}\">");
            page.AppendLine($"<h3><code>{Escape(module.Path)}.<b>{Escape(function.Name)}</b>({arguments})</code>"
                            + $" <span class=\"returns\">&rarr; {Escape(function.Returns)}</span></h3>");
            page.AppendLine($"<p>{Markup(function.Summary)}</p>");

            if (function.Parameters.Count > 0)
            {
                page.AppendLine("<table><thead><tr><th>Parameter</th><th>Type</th><th>Description</th></tr></thead><tbody>");
                foreach (var parameter in function.Parameters)
                {
                    page.AppendLine($"<tr><td><code>{Escape(parameter.Name)}</code></td>"
                                    + $"<td>{TypeLink(parameter.Type)}</td>"
                                    + $"<td>{Markup(parameter.Summary)}</td></tr>");
                }
                page.AppendLine("</tbody></table>");
            }
            page.AppendLine("</div>");
        }
        page.AppendLine("</section>");
    }

    private static void WriteTable(StringBuilder page, TableDoc table)
    {
        page.AppendLine($"<section id=\"{Anchor(table.Name)}\">");
        page.AppendLine($"<h2 class=\"table\">{Escape(table.Name)}</h2>");
        page.AppendLine($"<p>{Markup(table.Summary)}</p>");

        if (table.Shorthand is not null)
        {
            page.AppendLine($"<p class=\"note\">A bare string is shorthand for "
                            + $"<code>{{ {Escape(table.Shorthand)} = &lt;string&gt; }}</code>.</p>");
        }

        if (table.Given)
        {
            page.AppendLine("<p class=\"note\">Handed to your handler rather than written by you.</p>");
        }

        // A shape written by a script says what a key falls back to when it is left
        // out; one handed to a script says whether the key can be nil instead, since
        // nothing there is ever left out.
        var third = table.Given ? "Value" : "Default";
        page.AppendLine($"<table><thead><tr><th>Field</th><th>Type</th><th>{third}</th><th>Description</th></tr></thead><tbody>");
        foreach (var field in table.Fields)
        {
            page.AppendLine($"<tr><td><code>{Escape(field.Name)}</code></td>"
                            + $"<td>{TypeLink(field.Type)}</td><td>{Fallback(table, field)}</td>"
                            + $"<td>{Markup(field.Summary)}</td></tr>");
        }
        page.AppendLine("</tbody></table></section>");
    }

    /// <summary>What the third column says about one key, which depends on who writes it.</summary>
    private static string Fallback(TableDoc table, FieldDoc field) => (table.Given, field.Required) switch
    {
        (true, true) => "<span class=\"required\">always</span>",
        (true, false) => "<span class=\"absent\">may be nil</span>",
        (false, true) => "<span class=\"required\">required</span>",
        _ => field.Default is null ? "<span class=\"absent\">none</span>" : $"<code>{Escape(field.Default)}</code>",
    };

    private static void WriteEnum(StringBuilder page, EnumDoc enumeration)
    {
        page.AppendLine($"<section id=\"{Anchor(enumeration.Name)}\">");
        page.AppendLine($"<h2 class=\"enum\">{Escape(enumeration.Name)}</h2>");
        page.AppendLine($"<p>{Markup(enumeration.Summary)}</p>");
        page.AppendLine("<table><thead><tr><th>Value</th><th>Description</th></tr></thead><tbody>");
        foreach (var value in enumeration.Values)
        {
            page.AppendLine($"<tr><td><code>\"{Escape(value.Name)}\"</code></td>"
                            + $"<td>{Markup(value.Summary)}</td></tr>");
        }
        page.AppendLine("</tbody></table></section>");
    }

    /// <summary>Links a rendered type name back to its own section when one exists.</summary>
    private static string TypeLink(string type)
    {
        var bare = type.TrimEnd('?').Replace("[]", "");
        var suffix = Escape(type[bare.Length..]);
        return char.IsUpper(bare.FirstOrDefault())
            ? $"<a href=\"#{Anchor(bare)}\"><code>{Escape(bare)}</code></a>{suffix}"
            : $"<code>{Escape(type)}</code>";
    }

    /// <summary>Renders the only markup doc comments produce: backtick code spans.</summary>
    private static string Markup(string text) =>
        CodeSpan().Replace(Escape(text), match => $"<code>{match.Groups[1].Value}</code>");

    private static string Escape(string text) => WebUtility.HtmlEncode(text);

    private static string Anchor(string name) =>
        NonAnchor().Replace(name.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex CodeSpan();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAnchor();

    private const string Stylesheet = """
        :root {
          --bg: #ffffff; --fg: #1b1b1d; --muted: #5c5c66; --line: #e2e2e8;
          --accent: #2f6f4f; --code-bg: #f4f4f7; --nav-bg: #fafafc; --required: #9a3b2f;
        }
        :root:not([data-theme="light"]) { }
        @media (prefers-color-scheme: dark) {
          :root:not([data-theme="light"]) {
            --bg: #16161a; --fg: #e6e6ea; --muted: #9a9aa5; --line: #2c2c34;
            --accent: #7fc9a2; --code-bg: #202028; --nav-bg: #1b1b21; --required: #e08b7d;
          }
        }
        * { box-sizing: border-box; }
        body {
          margin: 0; background: var(--bg); color: var(--fg);
          font: 15px/1.6 system-ui, -apple-system, "Segoe UI", sans-serif;
          display: grid; grid-template-columns: 260px minmax(0, 1fr);
        }
        code, pre { font-family: ui-monospace, "SF Mono", Menlo, Consolas, monospace; font-size: 0.92em; }
        code { background: var(--code-bg); padding: 0.1em 0.35em; border-radius: 3px; }
        a { color: var(--accent); text-decoration: none; }
        a:hover { text-decoration: underline; }
        nav {
          background: var(--nav-bg); border-right: 1px solid var(--line);
          padding: 1.5rem 1rem; height: 100vh; position: sticky; top: 0; overflow-y: auto;
        }
        nav .brand { font-weight: 600; font-size: 1.1rem; margin-bottom: 1.5rem; }
        nav h2 { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.08em;
                 color: var(--muted); margin: 1.4rem 0 0.5rem; }
        nav ul { list-style: none; margin: 0; padding: 0; }
        nav li { margin: 0.25rem 0; }
        nav code { background: none; padding: 0; }
        main { padding: 2.5rem 3rem; max-width: 60rem; }
        h1 { font-size: 1.7rem; margin: 0 0 0.5rem; }
        .version { color: var(--muted); font-weight: 400; font-size: 1rem; }
        .lede { color: var(--muted); margin-top: 0; }
        section { margin-top: 3rem; }
        h2 { font-size: 1.3rem; padding-bottom: 0.4rem; border-bottom: 1px solid var(--line); }
        h2 code { background: none; padding: 0; }
        .item { margin: 1.75rem 0; padding-left: 1rem; border-left: 3px solid var(--line); }
        .item h3 { font-size: 1rem; font-weight: 500; margin: 0 0 0.4rem; }
        .item h3 code { background: none; padding: 0; }
        .returns { color: var(--muted); font-weight: 400; }
        .note { color: var(--muted); }
        table { border-collapse: collapse; width: 100%; margin: 0.75rem 0; display: block; overflow-x: auto; }
        th, td { text-align: left; padding: 0.45rem 0.7rem; border-bottom: 1px solid var(--line);
                 vertical-align: top; }
        th { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--muted); }
        .required { color: var(--required); font-size: 0.85em; }
        .absent { color: var(--muted); font-size: 0.85em; }
        @media (max-width: 780px) {
          body { grid-template-columns: 1fr; }
          nav { height: auto; position: static; border-right: none; border-bottom: 1px solid var(--line); }
          main { padding: 1.5rem; }
        }
        """;
}
