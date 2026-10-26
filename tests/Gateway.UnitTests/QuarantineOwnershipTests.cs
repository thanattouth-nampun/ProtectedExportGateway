using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;

public sealed class QuarantineOwnershipTests
{
    private const string Label="reviewed-label",Owner="owner@example.test";
    [Fact] public void Empty_explicit_grants_is_only_a_candidate_never_publishable()
    {
        Assert.Equal("OWNER_ONLY_OBSERVED_NOT_ELIGIBLE",QuarantineOwnershipRules.Evaluate(new(true,Label,Owner,[],false),Label,Owner));
        var report=new QuarantineOwnershipReport(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new('A',64),1,"review", "OWNER_ONLY_OBSERVED_NOT_ELIGIBLE");
        Assert.False(report.CanPublish);Assert.False(report.GatePassed);
    }
    [Theory][InlineData(null)][InlineData(false)]
    public void Unknown_or_unprotected_is_rejected(bool? state)=>Assert.Equal("PROTECTION_UNKNOWN_OR_UNSUPPORTED",
        QuarantineOwnershipRules.Evaluate(new(state,Label,Owner,[],false),Label,Owner));
    [Fact] public void Missing_grants_or_role_readback_rejects()
    {
        Assert.Equal("PROTECTION_UNKNOWN_OR_UNSUPPORTED",QuarantineOwnershipRules.Evaluate(new(true,Label,Owner,null,false),Label,Owner));
        Assert.Equal("PROTECTION_UNKNOWN_OR_UNSUPPORTED",QuarantineOwnershipRules.Evaluate(new(true,Label,Owner,[],null),Label,Owner));
    }
    [Theory][InlineData("other@example.test")][InlineData("OWNER@example.test")]
    public void No_inferred_or_case_normalized_owner_match(string observed)=>Assert.Equal("RMS_OWNER_MISMATCH",
        QuarantineOwnershipRules.Evaluate(new(true,Label,observed,[],false),Label,Owner));
    [Fact] public void Extra_recipient_role_or_wrong_label_rejects()
    {
        foreach(var observed in new ProtectedFileInspection[] {
            new(true,Label,Owner,[new(["other@example.test"],["VIEW"])],false),
            new(true,Label,Owner,[],true),new(true,"wrong",Owner,[],false)})
            Assert.Equal("LABEL_OR_OWNER_ONLY_GRANTS_REJECTED",QuarantineOwnershipRules.Evaluate(observed,Label,Owner));
    }
    [Fact] public void Directory_resolution_requires_both_tenant_and_oid()
    {
        var tenant=Guid.NewGuid();var oid=Guid.NewGuid();var entry=new ReviewedPortalIdentity(oid,"Owner",Owner,"review");
        var directory=new PortalIdentityDirectory(tenant,[entry]);
        Assert.Equal(entry,directory.Resolve(new(tenant,oid)));
        Assert.Null(directory.Resolve(new(Guid.NewGuid(),oid)));Assert.Null(directory.Resolve(new(tenant,Guid.NewGuid())));
    }
}
