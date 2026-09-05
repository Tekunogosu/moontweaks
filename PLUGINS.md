# Plugins

A plugin is a code mod of its own that adds bindings to every script MoonTweaks runs.
Scripts reach them under `plugin.<name>`, an editor completes them from a library the
server writes beside MoonTweaks's own, and MoonTweaks itself carries nothing about
any particular plugin. `plugins/xlib/` is the smallest one that works, binding a few
of XLib's skill functions to show the shape; `xlib-mtweaks`, a mod of its own, is the
full XLib plugin built the same way.

## The contract

`MoonTweaks.Api.IMoonTweaksPlugin`, implemented on one of the mod's `ModSystem`
classes:

```csharp
public sealed class MyPlugin : ModSystem, IMoonTweaksPlugin
{
    public string Name => "mine";                       // scripts reach plugin.mine
    public IEnumerable<object> Domains() => [new MyDomain(api)];
}
```

`Name` is a Lua identifier: lowercase letters, digits and underscores, starting with
a letter. `Domains()` hands over the objects carrying the bindings, and is called once
per run, so a server's startup and every dry-run check each get fresh instances.

It is first called while assets are loaded, which the game does before it runs any
mod's `StartServerSide`. Take the API in `Start(ICoreAPI)`, as the XLib plugin does,
and reach anything another mod only builds in its own server-side start at call time,
where a script's commands and event handlers run, rather than when the domain is made.

A domain is a class annotated the way MoonTweaks's own are: `[LuaModule("plugin.mine")]`
on the class, `[LuaFunction("name")]` on each method, a `ScriptOrigin` as every
method's first parameter, and a doc comment on everything. Table shapes a function
takes carry `[LuaTable]` and `[LuaField]`, and one MoonTweaks already declares, such
as a stack, is used as it is. `src/Api/Annotations.cs` documents each annotation and
`plugins/xlib/SkillsDomain.cs` shows them together.

`PluginContract.VERSION` names the version of this contract. It is raised on a change
a plugin built against the previous one would not survive.

## What MoonTweaks does with one

At startup it walks the loaded mod systems for those implementing the contract, so a
plugin registers nothing and needs no particular execute order. Declaring `moontweaks`
as a dependency in `modinfo.json` is what has the plugin's assembly loaded in time and
its references to `moontweaks.dll` resolved by the game.

Every module a plugin binds must sit at `plugin.<name>` or beneath it. A path outside
that, a path already bound by MoonTweaks or by another plugin, two plugins claiming one
name, or a name that is not a plugin name refuses the whole run: no script runs, and
the log names the plugin and the reason. The refusal is deliberate. A script written
against that plugin cannot tell a quietly dropped plugin from its own typo, and the
operator who installed it is who can act on the message.

`/moontweaks plugins` lists every plugin bound and the paths scripts reach it at.

## Editor support

The server renders `library/plugin.<name>.lua` from the plugin's assembly the same way
it renders `library/moontweaks.lua` from its own, so an editor completes and checks a
plugin's functions exactly as it does MoonTweaks's. Plugins are bound before the
libraries are written, so a refused plugin never describes itself. Summaries come from the compiler's
XML documentation, which the plugin ships beside its DLL in the zip; a plugin shipping
none is still bound and described, without descriptions, and the log says so. A
library is rewritten only when the bindings it describes have changed, and one left by
a plugin since removed is deleted.

## Packaging

The zip holds the plugin's DLL, its XML documentation and its `modinfo.json`. It must
not hold `moontweaks.dll` or the DLL of any mod it reaches into: the game loads every
mod's assemblies into one process and resolves references from the mods it has already
loaded, and a second copy of an assembly carrying mod systems breaks their loading.
`plugins/xlib/moontweaks-xlib.csproj` references both with `Private="false"` for that
reason, and `plugins/xlib/build.sh` is the whole packaging step.
