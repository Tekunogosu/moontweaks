using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>
/// Settings a server operator changes, read from <c>config.json</c> in the
/// MoonTweaks folder. Written with its defaults the first time a server starts,
/// so the file that documents the settings is the file that sets them.
/// </summary>
public sealed class MoonTweaksConfig
{
    /// <summary>Name of the settings file inside the MoonTweaks folder.</summary>
    public const string FILE_NAME = "config.json";

    private static readonly JsonSerializerOptions Format = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        // A setting written in the casing the documentation uses has to be the
        // setting that is read. Without this a key whose case does not match the
        // property is not a mistake the reader is told about: it binds to nothing
        // and the default stands, which reads to a server operator as the setting
        // having been ignored on purpose.
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Privilege a player must hold to run any <c>/moontweaks</c> command. Defaults
    /// to the one administrators hold, so nobody else can run them until a server
    /// says otherwise.
    /// </summary>
    public string CommandPrivilege { get; set; } = Privilege.controlserver;

    /// <summary>
    /// How many steps of block history <c>world.undo</c> can walk back through, kept
    /// per script. Each step holds every block that step wrote, so this is memory a
    /// server pays for whether or not anything is ever undone; a script that fills a
    /// large region is the case worth thinking about before raising it. One is the
    /// least it can be.
    /// </summary>
    public int UndoHistory { get; set; } = 5;

    /// <summary>
    /// Reads the settings, writing the defaults first when a server has none. Whatever
    /// goes wrong is reported and the defaults are used, so neither bad JSON nor a
    /// folder that will not be written to costs a server anything but its settings.
    /// </summary>
    public static MoonTweaksConfig Load(string folder, ILogger logger)
    {
        var path = Path.Combine(folder, FILE_NAME);

        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<MoonTweaksConfig>(File.ReadAllText(path), Format)
                       ?? new MoonTweaksConfig();
            }

            var defaults = new MoonTweaksConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, Format));
            logger.Notification("[moontweaks] wrote {0} with its defaults", FILE_NAME);
            return defaults;
        }
        catch (Exception error)
        {
            logger.Error("[moontweaks] {0} could not be read ({1}); using defaults", FILE_NAME, error.Message);
            return new MoonTweaksConfig();
        }
    }
}
