using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;

public sealed class ExcelReferenceProbeTests
{
    private static ProtectedFileInspection Reference => new(true,"label","owner@example.test",
        [new(["a@example.test"],["VIEW","DOCEDIT","EDIT"]),new(["owner@example.test"],["OWNER"])],false);
    [Fact] public void Exact_excel_reference_grants_match_with_owner_separate_from_ordinary_recipient()
    {
        Assert.True(Matches(Reference));
        Assert.True(Matches(Reference with {Grants=Reference.Grants!.Reverse().ToArray()}));
        Assert.False(ProtectedSourceRules.IsRecipientCopy(Reference,"label","owner@example.test","a@example.test"));
    }
    [Theory]
    [InlineData("missing-owner")][InlineData("another-owner")][InlineData("owner-changed")]
    [InlineData("extra-user")][InlineData("extra-recipient-right")][InlineData("recipient-owner")]
    [InlineData("extra-owner-right")][InlineData("unknown")][InlineData("roles")]
    public void Missing_or_unexpected_grants_reject(string fault)
    {
        var reference=Reference;
        var observed=fault switch {
            "missing-owner"=>reference with {Grants=[reference.Grants![0]]},
            "another-owner"=>reference with {Grants=[reference.Grants![0],new(["other@example.test"],["OWNER"])]},
            "owner-changed"=>reference with {Owner="a@example.test"},
            "extra-user"=>reference with {Grants=[..reference.Grants!,new(["b@example.test"],["VIEW"])]},
            "extra-recipient-right"=>reference with {Grants=[new(["a@example.test"],["VIEW","DOCEDIT","EDIT","PRINT"]),reference.Grants![1]]},
            "recipient-owner"=>reference with {Grants=[new(["a@example.test"],["OWNER"]),reference.Grants![1]]},
            "extra-owner-right"=>reference with {Grants=[reference.Grants![0],new(["owner@example.test"],["OWNER","EDIT"])]},
            "unknown"=>reference with {Grants=null}, _=>reference with {HasRoleGrants=true}
        };
        Assert.False(Matches(observed));
    }
    private static bool Matches(ProtectedFileInspection value)=>ProtectedSourceRules.IsExcelReferenceProbe(value,"label","owner@example.test","a@example.test");
}
