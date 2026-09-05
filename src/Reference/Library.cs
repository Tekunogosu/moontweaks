using System.Reflection;

namespace MoonTweaks.Reference;

/// <summary>
/// One type library an editor reads: what a mod's bindings look like from Lua,
/// rendered from the assembly that declares them. Written by a server at startup
/// for its own bindings and for every plugin's, so the library an author completes
/// against is always the one the running binary exposes.
/// </summary>
/// <param name="FileName">Name of the file in the library folder.</param>
/// <param name="Contents">The library, with the build marker in its header.</param>
/// <param name="Documented">
/// Whether the assembly shipped the compiler's XML documentation beside itself. One
/// that did not is described without its summaries.
/// </param>
public sealed record Library(string FileName, string Contents, bool Documented)
{
    /// <summary>Renders the library for one assembly's bindings.</summary>
    /// <param name="fileName">Name of the file in the library folder.</param>
    /// <param name="name">What declares the bindings, as the header names it.</param>
    /// <param name="version">Version of that mod.</param>
    /// <param name="assembly">The assembly holding the annotated bindings.</param>
    public static Library Of(string fileName, string name, string version, Assembly assembly)
    {
        var docs = XmlDocs.Beside(assembly);
        var api = new ApiReflector(assembly, docs ?? XmlDocs.None).Read(name, version);
        return new Library(fileName, LuaCatsWriter.Write(api), docs is not null);
    }
}
