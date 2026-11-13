using FortniteReplayReader.Models.NetFieldExports.Weapons;
using Unreal.Core.Attributes;
using Unreal.Core.Contracts;
using Unreal.Core.Models.Enums;

namespace FCT.ReplayModels;

/// <summary>
/// A treasure-hunt gizmo in someone's hands (golden firework, locators, extractor…). Each has its own blueprint; FCT
/// points them here through <see cref="Unreal.Core.ReplayAliases.Group"/> and reads the shared weapon fields.
/// </summary>
[NetFieldExportGroup(Path, minimalParseMode: ParseMode.Minimal)]
public sealed class AnyGizmo : BaseWeapon
{
    public const string Path = "FCT_AnyGizmo";
}

/// <summary>
/// The ability system component of a player state. FCT does not decode its abilities field-by-field (their names are
/// not exported): registering the component makes its ClassNetCache reach FCT, which reads the raw payload.
/// </summary>
[NetFieldExportClassNetCache("FortAbilitySystemComponentAthena_ClassNetCache", minimalParseMode: ParseMode.Minimal)]
public sealed class AbilitySystemCalls
{
    [NetFieldExportRPC("ActivatableAbilities", AbilitySystemState.Path, enablePropertyChecksum: false)]
    public AbilitySystemState? ActivatableAbilities { get; set; }
}

/// <summary>Replicated fields of the component (the same names an ability spec uses).</summary>
[NetFieldExportGroup(Path, minimalParseMode: ParseMode.Minimal)]
public sealed class AbilitySystemState : INetFieldExportGroup
{
    public const string Path = "/Script/FortniteGame.FortAbilitySystemComponentAthena";

    [NetFieldExport("ability", RepLayoutCmdType.PropertyObject)]
    public uint? Ability { get; set; }

    [NetFieldExport("SourceObject", RepLayoutCmdType.PropertyObject)]
    public uint? SourceObject { get; set; }

    [NetFieldExport("OwnerActor", RepLayoutCmdType.PropertyObject)]
    public uint? OwnerActor { get; set; }

    [NetFieldExport("AvatarActor", RepLayoutCmdType.PropertyObject)]
    public uint? AvatarActor { get; set; }

    [NetFieldExport("InputID", RepLayoutCmdType.PropertyInt)]
    public int? InputID { get; set; }
}
