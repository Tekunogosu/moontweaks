using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// The character traits a server's assets define, which gate a recipe behind a
/// character class. Sole owner of that list, so nothing checks a trait name
/// against a set it assembled for itself.
/// </summary>
/// <remarks>
/// Read from the <c>config/traits</c> assets rather than from the character system
/// that also reads them: that system fills its own registry once the server reaches
/// <see cref="Vintagestory.API.Server.EnumServerRunPhase.ModsAndConfigReady"/>, which
/// is after scripts have run and their recipes have been registered.
/// </remarks>
public sealed class TraitRegistry(ICoreAPI api)
{
    private IReadOnlySet<string>? codes;

    /// <summary>
    /// Every trait code, read on first use. Most scripts name no trait at all, so a
    /// run that never asks never opens the files.
    /// </summary>
    public IReadOnlySet<string> Codes => codes ??= Read();

    /// <summary>
    /// Every code across every origin. A file holds one trait or a list of them, and
    /// the game accepts both spellings, so both are read here.
    /// </summary>
    private IReadOnlySet<string> Read() =>
        api.Assets.GetMany<JToken>(api.Logger, "config/traits").Values
            .SelectMany(file => file is JArray many ? many.AsEnumerable() : new[] { file })
            .Select(trait => trait["code"]?.Value<string>())
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
}
