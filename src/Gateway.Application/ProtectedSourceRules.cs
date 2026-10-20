using Gateway.Domain;
using System.Collections.Frozen;

namespace Gateway.Application;

// Facts read from a fresh MIP handler, never copied from requested protection settings.
public sealed record ObservedGrant(string[] Users, string[] Rights);
public sealed record ProtectedFileInspection(bool? IsProtected, string? LabelId, string? Owner,
    ObservedGrant[]? Grants, bool? HasRoleGrants);

public static class ProtectedSourceRules
{
    // Office custom-permission candidate; never substitutes the production edit/save release profile.
    // Microsoft usage-rights table: Restricted Editor, excluding OBJMODEL in Office Windows 2408+.
    public static readonly IReadOnlySet<string> RestrictedEditorProbeRights = new[]
        { "VIEW", "DOCEDIT", "EDIT", "VIEWRIGHTSDATA", "REPLY", "REPLYALL", "FORWARD" }.ToFrozenSet(StringComparer.Ordinal);

    // Observed on the supplied Excel reference, not inferred from the generic permissions table.
    public static bool IsExcelReferenceProbe(ProtectedFileInspection observed, string label, string originalOwner, string recipient) =>
        recipient != originalOwner && Known(observed,label,originalOwner)
        && observed.Grants is {Length:2}
        && observed.Grants.Count(g=>g.Users is [var user] && user==originalOwner && g.Rights is ["OWNER"])==1
        && observed.Grants.Count(g=>g.Users is [var user] && user==recipient && g.Rights is {Length:3}
            && ReleasePolicy.EditSaveRights.SetEquals(g.Rights))==1;

    public static bool IsRestrictedEditorProbe(ProtectedFileInspection observed, string label, string originalOwner, string recipient) =>
        recipient != originalOwner && Known(observed, label, originalOwner)
        && observed.Grants is [{ Users: [var user], Rights: var rights }]
        && user == recipient && rights is not null && rights.Length == RestrictedEditorProbeRights.Count
        && RestrictedEditorProbeRights.SetEquals(rights);

    public static bool IsOwnerOnly(ProtectedFileInspection observed, string label, string owner) =>
        Known(observed, label, owner) && observed.Grants!.All(g =>
            g.Users is { Length: > 0 } && g.Rights is { Length: > 0 }
            && g.Rights.All(r => !string.IsNullOrWhiteSpace(r))
            && g.Users.All(user => user == owner));

    public static bool IsRecipientCopy(ProtectedFileInspection observed, string label, string originalOwner, string recipient) =>
        recipient != originalOwner && Known(observed, label, originalOwner)
        && observed.Grants is [{ Users: [var user], Rights: var rights }]
        && user == recipient && rights is not null && rights.Length == ReleasePolicy.EditSaveRights.Count
        && ReleasePolicy.EditSaveRights.SetEquals(rights);

    private static bool Known(ProtectedFileInspection o, string label, string owner) =>
        !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(owner)
        && o.IsProtected == true && o.LabelId == label && o.Owner == owner
        && o.Grants is not null && o.HasRoleGrants == false;
}
