using FortniteReplayReader.Models.NetFieldExports.Items;
using Unreal.Core.Attributes;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace FCT.ReplayModels;

/// <summary>
/// Any searchable container the library has no model for (new chest blueprints appear every season). FCT points such
/// groups here through <see cref="Unreal.Core.ReplayAliases.Group"/>, and reads them with the shared container fields.
/// </summary>
[NetFieldExportGroup(Path, minimalParseMode: ParseMode.Minimal)]
public sealed class AnyContainer : BaseContainer
{
    /// <summary>Not a game path: a name only FCT's aliases point to.</summary>
    public const string Path = "FCT_AnyContainer";
}

/// <summary>
/// The "FireLootCannon" call of loot cannons: who used the cannon. Each cannon blueprint has its own ClassNetCache; FCT
/// points all of them at this one through <see cref="Unreal.Core.ReplayAliases.ClassNetCache"/>.
/// </summary>
[NetFieldExportClassNetCache(CachePath, minimalParseMode: ParseMode.Minimal)]
public sealed class LootCannonCalls
{
    public const string CachePath = "FCT_LootCannon_ClassNetCache";

    [NetFieldExportRPC("FireLootCannon", LootCannonShot.Path, isFunction: true)]
    public LootCannonShot? FireLootCannon { get; set; }
}

/// <summary>Parameters of FireLootCannon (names as the game sends them).</summary>
[NetFieldExportGroup(Path, minimalParseMode: ParseMode.Minimal)]
public sealed class LootCannonShot : INetFieldExportGroup
{
    public const string Path = "/LootCannon/Gameplay/BGA_LootCannon.BGA_LootCannon_C:FireLootCannon";

    [NetFieldExport("InteractingPawn", RepLayoutCmdType.PropertyObject)]
    public uint? InteractingPawn { get; set; }

    [NetFieldExport("LootTierToFire", RepLayoutCmdType.Ignore)]
    public DebuggingObject? LootTierToFire { get; set; }
}

/// <summary>
/// The golden chest the treasure hunt spawns when a chest pays out a featured reward: it carries the prize id, who
/// opened the chest and which container it was.
/// </summary>
[NetFieldExportGroup(Path, minimalParseMode: ParseMode.Minimal)]
public sealed class TreasurePayout : INetFieldExportGroup
{
    public const string Path = "/DelilahTreasureHuntGameplay/Gameplay/Actors/BGA_DelilahTreasureHuntAnimatedChest.BGA_DelilahTreasureHuntAnimatedChest_C";

    [NetFieldExport("PrizeTemplateId", RepLayoutCmdType.Property)]
    public DebuggingObject? PrizeTemplateId { get; set; }

    [NetFieldExport("OpeningPawn", RepLayoutCmdType.PropertyObject)]
    public uint? OpeningPawn { get; set; }

    [NetFieldExport("Instigator", RepLayoutCmdType.PropertyObject)]
    public uint? Instigator { get; set; }

    [NetFieldExport("SourceContainer", RepLayoutCmdType.PropertyObject)]
    public uint? SourceContainer { get; set; }

    [NetFieldExport("bSuccess", RepLayoutCmdType.PropertyBool)]
    public bool? bSuccess { get; set; }

    [NetFieldExport("OwnerPersistentID", RepLayoutCmdType.Ignore)]
    public DebuggingObject? OwnerPersistentID { get; set; }

    [NetFieldExport("Tier", RepLayoutCmdType.Property)]
    public DebuggingObject? Tier { get; set; }

    [NetFieldExport("TierData", RepLayoutCmdType.PropertyObject)]
    public uint? TierData { get; set; }
}
