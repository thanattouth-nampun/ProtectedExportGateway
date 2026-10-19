using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;

public sealed class ProtectedSourceTests
{
    private const string Label = "test-udp-label";
    private const string Owner = "owner@example.test";
    private const string A = "a@example.test";
    private static ProtectedFileInspection Source => new(true,Label,Owner,[],false);
    private static ProtectedFileInspection Copy => new(true,Label,Owner,[new([A],["VIEW","DOCEDIT","EDIT"])],false);

    [Fact] public void Implicit_owner_or_explicit_owner_grant_is_a_candidate_not_a_live_proof()
    {
        Assert.True(ProtectedSourceRules.IsOwnerOnly(Source,Label,Owner));
        Assert.True(ProtectedSourceRules.IsOwnerOnly(Source with { Grants=[new([Owner],["OWNER"])] },Label,Owner));
    }
    [Theory]
    [InlineData("unknown-protection")][InlineData("unprotected")][InlineData("label")][InlineData("owner")]
    [InlineData("unknown-grants")][InlineData("unknown-roles")][InlineData("roles")][InlineData("other-user")]
    [InlineData("empty-users")][InlineData("empty-rights")]
    public void Source_unknown_or_additional_grants_are_rejected(string fault)
    {
        var o = fault switch {
            "unknown-protection"=>Source with {IsProtected=null}, "unprotected"=>Source with {IsProtected=false},
            "label"=>Source with {LabelId="other"}, "owner"=>Source with {Owner=A},
            "unknown-grants"=>Source with {Grants=null}, "unknown-roles"=>Source with {HasRoleGrants=null},
            "roles"=>Source with {HasRoleGrants=true}, "other-user"=>Source with {Grants=[new([Owner,A],["VIEW"])]},
            "empty-users"=>Source with {Grants=[new([],["VIEW"])]}, _=>Source with {Grants=[new([Owner],[])]}
        };
        Assert.False(ProtectedSourceRules.IsOwnerOnly(o,Label,Owner));
    }
    [Fact] public void One_recipient_with_candidate_rights_and_original_owner_matches()
        => Assert.True(ProtectedSourceRules.IsRecipientCopy(Copy,Label,Owner,A));
    [Theory]
    [InlineData("accumulated")][InlineData("export")][InlineData("owner-changed")][InlineData("roles")]
    [InlineData("unknown-grants")][InlineData("duplicate-rights")]
    public void Copies_reject_accumulation_excess_rights_unknown_and_ownership_change(string fault)
    {
        var o = fault switch {
            "accumulated"=>Copy with {Grants=[new([A,"b@example.test"],["VIEW","DOCEDIT","EDIT"])]},
            "export"=>Copy with {Grants=[new([A],["VIEW","DOCEDIT","EDIT","EXPORT"])]},
            "owner-changed"=>Copy with {Owner=A}, "roles"=>Copy with {HasRoleGrants=true},
            "duplicate-rights"=>Copy with {Grants=[new([A],["VIEW","DOCEDIT","EDIT","EDIT"])]},
            _=>Copy with {Grants=null}
        };
        Assert.False(ProtectedSourceRules.IsRecipientCopy(o,Label,Owner,A));
    }
}
