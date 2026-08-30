namespace MoonTweaks.Api;

// What a script writes to reach a set of slots, and what it is told back about one.

/// <summary>Which of a player's several inventories is meant.</summary>
public enum EnumBagKind
{
    /// <summary>The quick slots along the bottom of the screen.</summary>
    Hotbar,

    /// <summary>What their bags hold.</summary>
    Backpack,

    /// <summary>What they are wearing, armour and clothing alike.</summary>
    Character,

    /// <summary>The crafting grid they have open.</summary>
    CraftingGrid,

    /// <summary>What they have picked up with the cursor and not yet put down.</summary>
    Mouse,

    /// <summary>The creative inventory, which only exists in creative mode.</summary>
    Creative,
}

/// <summary>
/// Which set of slots to act on. Exactly one of the three ways of naming one is
/// written: a player and which of their inventories, a block position, or an entity.
/// </summary>
/// <remarks>
/// One shape rather than three families of function, because everything a script does
/// to a chest it also does to a backpack. What differs is only where the slots are,
/// so that is the only thing this says.
///
/// A block position names whatever container stands there — a chest, a barrel, a
/// crate. A position holding a block that is not a container is refused by name
/// rather than answered with nothing.
/// </remarks>
[LuaTable("Where")]
public sealed class WhereSpec
{
    /// <summary>Identifier of the player whose inventory is meant, as an event gives it.</summary>
    [LuaField("player")]
    public string? Player { get; set; }

    /// <summary>
    /// Which of that player's inventories. Their bags when omitted, which is what
    /// "their inventory" usually means.
    /// </summary>
    [LuaField("which", Default = "\"backpack\"")]
    public EnumBagKind? Which { get; set; }

    /// <summary>Identifier of the entity whose inventory is meant, as a search gives it.</summary>
    [LuaField("entity")]
    public double? Entity { get; set; }

    /// <summary>Position of the container, east to west.</summary>
    [LuaField("x")]
    public int? X { get; set; }

    /// <summary>Position of the container, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int? Y { get; set; }

    /// <summary>Position of the container, north to south.</summary>
    [LuaField("z")]
    public int? Z { get; set; }
}

/// <summary>One slot of an inventory, and what is standing in it.</summary>
[LuaTable("Slot", Given = true)]
public sealed class SlotPayload
{
    /// <summary>
    /// Which slot this is, counting from 1. Lua counts from 1 and so does this, so a
    /// slot number reads the same as any other index a script handles.
    /// </summary>
    [LuaField("slot")]
    public int Slot { get; init; }

    /// <summary>Asset code of what is in it.</summary>
    [LuaField("code")]
    public string Code { get; init; } = "";

    /// <summary>How many of it there are.</summary>
    [LuaField("quantity")]
    public int Quantity { get; init; }

    /// <summary>How many would fit in this slot at once.</summary>
    [LuaField("maxStackSize")]
    public int MaxStackSize { get; init; }

    /// <summary>
    /// What it is called, as the game would name it to a player. Read from the
    /// server's own language files, which are not necessarily the reader's.
    /// </summary>
    [LuaField("name")]
    public string Name { get; init; } = "";
}
