using Gateway.Application;
using Gateway.Domain;
using Xunit;

namespace Gateway.UnitTests;

public sealed class RestrictedEditorProbeTests
{
    private static ProtectedFileInspection Copy(string[] rights) => new(true,"label","owner@example.test",
        [new(["a@example.test"],rights)],false);
    [Fact] public void Exact_probe_rights_match_without_changing_production_profile()
    {
        var observed = Copy(ProtectedSourceRules.RestrictedEditorProbeRights.ToArray());
        Assert.True(ProtectedSourceRules.IsRestrictedEditorProbe(observed,"label","owner@example.test","a@example.test"));
        Assert.False(ProtectedSourceRules.IsRecipientCopy(observed,"label","owner@example.test","a@example.test"));
        Assert.Equal(3,ReleasePolicy.EditSaveRights.Count);
    }
    [Theory]
    [InlineData("PRINT")][InlineData("EXTRACT")][InlineData("EXPORT")][InlineData("OWNER")][InlineData("EDITRIGHTSDATA")][InlineData("OBJMODEL")][InlineData("EDIT")]
    public void Additional_or_duplicate_right_rejects(string extra) => Assert.False(ProtectedSourceRules.IsRestrictedEditorProbe(
        Copy([..ProtectedSourceRules.RestrictedEditorProbeRights,extra]),"label","owner@example.test","a@example.test"));
    [Fact] public void Missing_right_rejects() => Assert.False(ProtectedSourceRules.IsRestrictedEditorProbe(
        Copy(["VIEW","DOCEDIT","EDIT"]),"label","owner@example.test","a@example.test"));
    [Fact] public void Additional_recipient_rejects()
    {
        var observed = Copy(ProtectedSourceRules.RestrictedEditorProbeRights.ToArray()) with {
            Grants=[new(["a@example.test","b@example.test"],ProtectedSourceRules.RestrictedEditorProbeRights.ToArray())] };
        Assert.False(ProtectedSourceRules.IsRestrictedEditorProbe(observed,"label","owner@example.test","a@example.test"));
    }
}
