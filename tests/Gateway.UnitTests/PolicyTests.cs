using Gateway.Domain;
using Xunit;

namespace Gateway.UnitTests;

public class PolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-19T02:00:00Z");
    private static readonly PrincipalId A = new(Guid.NewGuid(), Guid.NewGuid());
    private static readonly PrincipalId B = new(A.TenantId, Guid.NewGuid());
    private static readonly PrincipalId Owner = new(A.TenantId, Guid.NewGuid());
    private static readonly BlobBinding Blob = new("candidate", "immutable-version", "etag", new string('A', 64), 100);
    private static CopySnapshot Snapshot => new(A, Guid.NewGuid(), Guid.NewGuid(), Blob, Owner, "label", "edit-save-v1",
        new(A, "a@example.test", "mapping-v1", Now, true), "policy-v1", "processor@example.test");
    private static OutputObservation Evidence => new("label", true, Blob.Sha256, Blob.VersionId, Blob.ETag,
        ["a@example.test"], ["VIEW", "DOCEDIT", "EDIT"], "processor@example.test", "test-only", Now);
    private static ScanEvidence Clean => new(ExecutionMode.LiveMip, ScanVerdict.Clean, Blob.Sha256, Blob.Sha256,
        "test-only", "engine", "signatures", Now, Now, "run");
    [Fact] public void Complete_test_inputs_satisfy_release_predicate() => Assert.True(ReleasePolicy.CanReleaseCopy(ExecutionMode.LiveMip, Snapshot, Blob, Evidence, Now, true, true, true));
    [Fact] public void Simulated_never_releases() => Assert.False(ReleasePolicy.CanReleaseCopy(ExecutionMode.Simulated, Snapshot, Blob, Evidence, Now, true, true, true));
    [Theory]
    [InlineData("recipient")][InlineData("rights")][InlineData("label")][InlineData("hash")][InlineData("blob-version")][InlineData("etag")]
    [InlineData("unknown-protection")][InlineData("owner")][InlineData("stale")][InlineData("unknown-recipients")]
    public void Any_observed_mismatch_blocks_release(string field)
    {
        var evidence = field switch {
            "recipient" => Evidence with { Recipients = ["a@example.test", "b@example.test"] },
            "rights" => Evidence with { Rights = ["VIEW", "DOCEDIT", "EDIT", "EXPORT"] },
            "label" => Evidence with { LabelId = "other" }, "hash" => Evidence with { Sha256 = new string('B', 64) },
            "blob-version" => Evidence with { BlobVersionId = "latest" }, "etag" => Evidence with { ETag = "other" },
            "unknown-protection" => Evidence with { Protected = null }, "owner" => Evidence with { ProtectionOwner = "a@example.test" },
            "stale" => Evidence with { InspectedAt = Now.AddMinutes(-16) }, _ => Evidence with { Recipients = null }
        };
        Assert.False(ReleasePolicy.CanReleaseCopy(ExecutionMode.LiveMip, Snapshot, Blob, evidence, Now, true, true, true));
    }
    [Theory][InlineData(false,true,true)][InlineData(true,false,true)][InlineData(true,true,false)]
    public void Current_authorization_real_intake_and_transaction_are_required(bool auth, bool intake, bool commit) =>
        Assert.False(ReleasePolicy.CanReleaseCopy(ExecutionMode.LiveMip, Snapshot, Blob, Evidence, Now, auth, intake, commit));
    [Fact] public void Recipient_mapping_cannot_cross_users_or_be_stale()
    {
        var mapping = Snapshot.Recipient;
        Assert.False(mapping.IsFresh(B, Now, TimeSpan.FromMinutes(15)));
        Assert.False(mapping.IsFresh(A, Now.AddMinutes(16), TimeSpan.FromMinutes(15)));
    }
    [Fact] public void Fingerprint_binds_requester_source_rights_and_mapping()
    {
        var a = Snapshot;
        foreach (var b in new[] { a with { Requester = B }, a with { RightsVersion = "new" },
            a with { Source = Blob with { VersionId = "new" } }, a with { Recipient = a.Recipient with { Version = "new" } } })
            Assert.NotEqual(a.Fingerprint(), b.Fingerprint());
    }
    [Fact] public void Auditor_owner_or_expired_job_cannot_download()
    {
        var snapshot = Snapshot;
        Assert.False(ReleasePolicy.CanDownload(B, snapshot, JobState.Ready, ExecutionMode.LiveMip, Now, Now.AddHours(1), true, true, true));
        Assert.False(ReleasePolicy.CanDownload(Owner, snapshot, JobState.Ready, ExecutionMode.LiveMip, Now, Now.AddHours(1), true, true, true));
        Assert.False(ReleasePolicy.CanDownload(A, snapshot, JobState.Ready, ExecutionMode.LiveMip, Now, Now, true, true, true));
        Assert.False(ReleasePolicy.CanDownload(A, snapshot, JobState.Ready, ExecutionMode.LiveMip, Now, Now.AddHours(1), false, true, true));
    }
    [Fact] public void State_machine_cannot_skip_verification_or_publish_simulation()
    {
        var simulated = new ExportStateMachine(ExecutionMode.Simulated);
        Assert.Throws<InvalidOperationException>(() => simulated.Move(JobState.Ready, true));
        simulated.Move(JobState.Preparing); simulated.Move(JobState.Labeling); simulated.Move(JobState.Verifying);
        Assert.Throws<InvalidOperationException>(() => simulated.Move(JobState.Publishing)); simulated.Move(JobState.Simulated);
        var live = new ExportStateMachine(ExecutionMode.LiveMip);
        live.Move(JobState.Preparing); live.Move(JobState.Labeling); live.Move(JobState.Verifying); live.Move(JobState.Publishing);
        Assert.Throws<InvalidOperationException>(() => live.Move(JobState.Ready));
    }
    [Theory][InlineData(ScanVerdict.Unknown)][InlineData(ScanVerdict.Remediated)][InlineData(ScanVerdict.Infected)]
    [InlineData(ScanVerdict.Error)][InlineData(ScanVerdict.Timeout)][InlineData(ScanVerdict.Disabled)][InlineData(ScanVerdict.NotScanned)]
    public void Non_clean_scan_never_eligible(ScanVerdict verdict) => Assert.False((Clean with { Verdict = verdict }).IsTrustedClean(Blob.Sha256, Now));
    [Fact] public void Fake_changed_or_stale_scan_never_eligible()
    {
        Assert.False((Clean with { Mode = ExecutionMode.Simulated }).IsTrustedClean(Blob.Sha256, Now));
        Assert.False((Clean with { AfterHash = new string('B', 64) }).IsTrustedClean(Blob.Sha256, Now));
        Assert.False((Clean with { IntelligenceAt = Now.AddHours(-25) }).IsTrustedClean(Blob.Sha256, Now));
    }
    [Fact] public void Draft_pointer_and_publish_withdraw_current_checks()
    {
        var doc = new Document(Guid.NewGuid(), Owner, "Plan", "Synthetic");
        doc.Edit(Owner, true, doc.Revision, "Plan", "Synthetic", [A, B], new HashSet<PrincipalId> { A, B });
        var v1 = new ValidatedVersion(Guid.NewGuid(), Blob, Clean, Blob.Sha256, "BoundedUnprotectedXlsx-v1", IntakeState.Eligible);
        doc.AddVersion(Owner, true, doc.Revision, v1); doc.Publish(Owner, true, doc.Revision, v1.Id, Now);
        var v2 = v1 with { Id = Guid.NewGuid(), State = IntakeState.AwaitingValidation };
        doc.AddVersion(Owner, true, doc.Revision, v2);
        Assert.Equal(v1.Id, doc.PublishedVersionId); Assert.True(doc.CanRequest(A, true, v1.Id, Now));
        Assert.Throws<InvalidOperationException>(() => doc.Publish(Owner, true, doc.Revision, v2.Id, Now));
        Assert.Throws<InvalidOperationException>(() => doc.Withdraw(Owner, true, doc.Revision - 1));
        Assert.Throws<KeyNotFoundException>(() => doc.Withdraw(A, true, doc.Revision));
        Assert.Throws<UnauthorizedAccessException>(() => doc.Withdraw(Owner, false, doc.Revision));
        doc.Withdraw(Owner, true, doc.Revision); Assert.False(doc.CanRequest(A, true, v1.Id, Now));
    }
    [Fact] public void Simulated_scan_cannot_be_published()
    {
        var doc = new Document(Guid.NewGuid(), Owner, "Plan", "Synthetic");
        var v = new ValidatedVersion(Guid.NewGuid(), Blob, Clean with { Mode = ExecutionMode.Simulated }, Blob.Sha256, "BoundedUnprotectedXlsx-v1", IntakeState.Eligible);
        doc.AddVersion(Owner, true, doc.Revision, v);
        Assert.Throws<InvalidOperationException>(() => doc.Publish(Owner, true, doc.Revision, v.Id, Now));
    }
}
