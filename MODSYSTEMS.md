# What this mod reaches inside other mods

Everything listed here is another mod's internals rather than the game's versioned
API. The API deprecates before it removes and a break in it fails the build with a
warning first; a mod system does neither, so a rename in a game update reaches a
server as a failed build here or, worse, as a script that stops working.

**This is the list to walk after every game update.** Each entry names the type, the
members called and the file calling them, so checking one is opening the decompiled
type and confirming the member is still there with the same shape.

`examples/scripts/diagnostics/55-survival.lua` exercises every one of these against a
running server. Running the diagnostics suite after an update answers the same
question this list asks, from the other end.

## How it is reached

`IModLoader.GetModSystem<T>()`, with the types referenced at compile time rather than
looked up by name. A rename therefore fails the build here rather than failing a
script on somebody's server, which is the whole reason to prefer it: the failure
lands on whoever can fix it, before release.

That is affordable because the coupling already exists and always has. The recipe
kinds are the survival mod's own types — `BarrelRecipe`, `AlloyRecipe`,
`CookingRecipe`, `RecipeRegistrySystem` — so MoonTweaks has never run on a server
without it. `src/GameSystems/GameSystems.cs` is the single place any of it is asked
for, and it turns a system that is not there into a failure naming the mod.

## Vintagestory.GameContent.WeatherSystemServer

Declared by `VSEssentials.dll`, whose mod id is **`game`**. Reached from
`src/GameSystems/WeatherDomain.cs`, bound as `moontweaks.weather`.

| Member | Declared on | Bound as |
| --- | --- | --- |
| `GetPrecipitation(double, double, double)` | `WeatherSystemBase` | `weather.precipitation` |
| `GetPrecipitationState(Vec3d)` | `WeatherSystemBase` | `weather.falling` |
| `GetEnvironmentWetness(BlockPos, double)` | `WeatherSystemBase` | `weather.wetness` |
| `OverridePrecipitation` (get and set) | `WeatherSystemBase` | `weather.setPrecipitation`, `clearPrecipitation`, `overridden` |
| `SpawnLightningFlash(Vec3d)` | `WeatherSystemBase` | `weather.lightning` |

Also read: `PrecipitationState.Level`, `.ParticleSize`, `.Type`, and the values of
`EnumPrecipitationType` — which `EnumFallingKind` mirrors by name, so a value added
or renamed there fails at the point the name is matched.

## Vintagestory.GameContent.SystemTemporalStability

Declared by `VSSurvivalMod.dll`, whose mod id is **`survival`**. Reached from
`src/GameSystems/StabilityDomain.cs`, bound as `moontweaks.stability`.

| Member | Bound as |
| --- | --- |
| `GetTemporalStability(double, double, double)` | `stability.at` |
| `StormData` | `stability.storm` |

Also read, off `TemporalStormRunTimeData`: `nowStormActive`, `stormGlitchStrength`,
`nextStormStrength`, `nextStormTotalDays`, `stormActiveTotalDays`. Those are public
fields with lowercase names rather than properties, which is a shape worth
re-checking rather than assuming. The values of `EnumTempStormStrength` are mirrored
by `EnumStormKind`, matched by name.

## Vintagestory.GameContent.ModSystemBlockReinforcement

Declared by `VSSurvivalMod.dll`, mod id **`survival`**. Reached from
`src/GameSystems/ReinforceDomain.cs`, bound as `moontweaks.reinforce`.

| Member | Bound as |
| --- | --- |
| `GetReinforcment(BlockPos)` | `reinforce.at` |
| `IsReinforced(BlockPos)` | `reinforce.isReinforced` |
| `StrengthenBlock(BlockPos, IPlayer, int)` | `reinforce.strengthen` |
| `ConsumeStrength(BlockPos, int)` | `reinforce.consume` |
| `ClearReinforcement(BlockPos)` | `reinforce.clear` |
| `IsLockedForInteract(BlockPos, IPlayer)` | `reinforce.isLockedFor` |

`GetReinforcment` is spelled as the game spells it, missing an `e`. A game update
correcting that spelling is a break like any other, and would be an easy one to miss
by reading past it.

Also read, off `BlockReinforcement`: `Strength`, `PlayerUID`, `LastPlayername`,
`Locked`, `LockedByItemCode`, `GroupUid`, `LastGroupname`.

`StrengthenBlock` answers false rather than throwing where the block has no
`BlockBehaviorReinforcable`, which is the game's own rule for its own command and is
what `reinforce.strengthen` passes back.

## Vintagestory.GameContent.RecipeRegistrySystem

Declared by `VSSurvivalMod.dll`, mod id **`survival`**. Reached from
`src/Recipes/RecipeRegistry.cs` and `src/Host/ClientSyncProbe.cs`, and it predates
everything above: this is the coupling the recipe kinds are built on.

Its recipe lists are reached by name through the game's own
`IWorldAccessor.GetRecipeRegistry`, so what wants checking is the registry codes
rather than the type's members. `COVERAGE.md` records which kinds are bound.
