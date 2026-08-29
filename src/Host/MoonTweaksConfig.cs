using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonTweaks.Scripting;
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
    public const string FileName = "config.json";

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
    /// Reads the settings, writing the defaults first when a server has none. A file
    /// that cannot be read is reported and the defaults used, so bad JSON costs a
    /// server its settings rather than its startup.
    /// </summary>
    public static MoonTweaksConfig Load(string folder, ILogger logger)
    {
        return Read(folder, logger);
    }

    /// <summary>Reads the file, or writes and returns the defaults when there is none.</summary>
    private static MoonTweaksConfig Read(string folder, ILogger logger)
    {
        var path = Path.Combine(folder, FileName);

        if (!File.Exists(path))
        {
            var defaults = new MoonTweaksConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, Format));
            logger.Notification("[moontweaks] wrote {0} with its defaults", FileName);
            return defaults;
        }

        try
        {
            return JsonSerializer.Deserialize<MoonTweaksConfig>(File.ReadAllText(path), Format)
                   ?? new MoonTweaksConfig();
        }
        catch (JsonException error)
        {
            logger.Error("[moontweaks] {0} could not be read ({1}); using defaults", FileName, error.Message);
            return new MoonTweaksConfig();
        }
    }
}
