namespace Gateway.Domain;

public enum PublicationStatus { Draft, Published, Withdrawn }
public enum IntakeState { Uploading, AwaitingValidation, Scanning, Validating, Eligible, Rejected, ValidationFailed }
public enum ScanVerdict { Unknown, Clean, Infected, Remediated, Error, Timeout, NotScanned, Disabled }
public sealed record ScanEvidence(ExecutionMode Mode, ScanVerdict Verdict, string BeforeHash,
    string AfterHash, string Provider, string EngineVersion, string IntelligenceVersion,
    DateTimeOffset IntelligenceAt, DateTimeOffset CompletedAt, string RunId)
{
    public bool IsTrustedClean(string hash, DateTimeOffset now) => Mode == ExecutionMode.LiveMip
        && Verdict == ScanVerdict.Clean && BeforeHash == hash && AfterHash == hash
        && hash.Length == 64 && hash.All(Uri.IsHexDigit)
        && !string.IsNullOrWhiteSpace(Provider) && !string.IsNullOrWhiteSpace(EngineVersion)
        && !string.IsNullOrWhiteSpace(IntelligenceVersion) && !string.IsNullOrWhiteSpace(RunId)
        && IntelligenceAt <= CompletedAt && CompletedAt <= now
        && CompletedAt - IntelligenceAt <= TimeSpan.FromHours(24);
}
public sealed record ValidatedVersion(Guid Id, BlobBinding Source, ScanEvidence Scan,
    string ValidationHash, string ValidationProfile, IntakeState State)
{
    public bool IsEligible(DateTimeOffset now) => State == IntakeState.Eligible && Source.IsValid
        && Scan.IsTrustedClean(Source.Sha256, now) && ValidationHash == Source.Sha256
        && ValidationProfile == "BoundedUnprotectedXlsx-v1";
}
// Aggregate enforces invariants; persistence must commit aggregate changes and audit in one transaction.
public sealed class Document(Guid id, PrincipalId owner, string title, string purpose)
{
    public Guid Id { get; } = id;
    public PrincipalId Owner { get; } = owner;
    public string Title { get; private set; } = title;
    public string Purpose { get; private set; } = purpose;
    public long Revision { get; private set; } = 1;
    public PublicationStatus Status { get; private set; } = PublicationStatus.Draft;
    public Guid? PublishedVersionId { get; private set; }
    private readonly HashSet<PrincipalId> acl = [];
    private readonly Dictionary<Guid, ValidatedVersion> versions = [];
    public bool CanRequest(PrincipalId requester, bool libraryReader, Guid version, DateTimeOffset now) =>
        libraryReader && requester.TenantId == Owner.TenantId && acl.Contains(requester)
        && Status == PublicationStatus.Published && PublishedVersionId == version
        && versions.TryGetValue(version, out var source) && source.IsEligible(now);
    public void Edit(PrincipalId actor, bool publisher, long revision, string title, string purpose,
        IEnumerable<PrincipalId> requesters, IReadOnlySet<PrincipalId> allowlist)
    {
        Authorize(actor, publisher, revision);
        var entries = requesters.ToHashSet();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || string.IsNullOrWhiteSpace(purpose)
            || purpose.Length > 1000 || entries.Count > 50
            || entries.Any(p => p.TenantId != Owner.TenantId || !allowlist.Contains(p)))
            throw new ArgumentException("REQUEST_INVALID");
        Title = title; Purpose = purpose; acl.Clear(); acl.UnionWith(entries); Revision++;
    }
    public void AddVersion(PrincipalId actor, bool publisher, long revision, ValidatedVersion version)
    {
        Authorize(actor, publisher, revision);
        if (!versions.TryAdd(version.Id, version)) throw new InvalidOperationException("VERSION_EXISTS");
        Revision++; // Deliberately preserve published pointer while a new draft is pending.
    }
    public void Publish(PrincipalId actor, bool publisher, long revision, Guid version, DateTimeOffset now)
    {
        Authorize(actor, publisher, revision);
        if (!versions.TryGetValue(version, out var candidate) || !candidate.IsEligible(now))
            throw new InvalidOperationException("VERSION_NOT_ELIGIBLE");
        PublishedVersionId = version; Status = PublicationStatus.Published; Revision++;
    }
    public void Withdraw(PrincipalId actor, bool publisher, long revision)
    {
        Authorize(actor, publisher, revision); Status = PublicationStatus.Withdrawn; Revision++;
    }
    private void Authorize(PrincipalId actor, bool publisher, long revision)
    {
        if (!publisher) throw new UnauthorizedAccessException("PUBLISHER_REQUIRED");
        if (actor != Owner) throw new KeyNotFoundException("DOCUMENT_NOT_FOUND");
        if (Revision != revision) throw new InvalidOperationException("REVISION_CONFLICT");
    }
}
