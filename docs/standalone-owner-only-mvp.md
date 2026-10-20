# ADR-013 — Standalone owner-only source and authorized per-recipient copies

20 October update: [ADR-014](member-self-service-workspace.md) replaces Publisher/pre-enrolled-owner empty-draft enrollment with a workspace for every verified internal tenant Member. Source ownership/protection/intake/release conditions below remain; Member status is not RMS ownership proof.

Decision accepted by the user on 19 October 2026. This document supersedes conflicting v4.2 unprotected-source-only library assumptions in docs/01–15 and AGENTS.md. ADR-012 (no 3D), both product modules, the adopted stack and all unaffected release/security/Azure gates remain in force. Existing evidence is historical evidence for its original scope, not acceptance of this new path.

## Product boundary

Standalone MVP: no SharePoint, OneDrive, Graph file connector, external content catalog or directory synchronization. Retain Entra OIDC, MIP/Purview, Azure data services/hosting and managed identities. Standalone does not mean anonymous, offline or disconnected from identity/protection services. HR synthetic report exports remain; this decision changes library intake, not the report module.

The owner uploads a bounded synthetic XLSX already protected with an approved user-defined-permissions label and ordinary access restricted to the original RMS owner. The source remains immutable. The owner controls a portal ACL of verified same-tenant requester identities. A requester receives a fresh protected copy only after portal authorization. A/B never gain direct source access or accumulate across copies. No arbitrary protected-file conversion, password decryption, general relabeling or plaintext download is added.

Two independent permission planes:

| Plane | Authority |
|---|---|
| Portal visibility/request eligibility | Publisher + verified business owner edits server ACL; backend enforces tid/oid, current publication/version and expiry. Login alone is insufficient. Initially use a bounded server-managed same-tenant allowlist; no directory-search connector is implied. |
| Source/output RMS protection | SDK must independently observe source label/owner/grants, use an explicitly authorized processor identity path, and inspect each newly committed output. The recipient is an ordinary editor-profile user, never delegated protection owner. |

Original creator, document Author metadata, uploader, portal business owner, RMS owner/issuer, processor and recipient are separate concepts. For the initial protected-source MVP, the uploader's trusted identity-to-RMS-address mapping must match the independently observed original RMS owner before source acceptance/owner management. Filename/Author fields and a caller-provided address cannot establish that binding. Processor on-behalf-of rights do not establish uploader ownership either.

Owner access must remain on outputs; A-only/B-only refers to ordinary recipients, excluding the retained original RMS owner and documented privileged recovery identities. This is not protection against the content owner or authorized tenant recovery administrators.

## Revised dependency order

1. Scope/docs/UI adjustment and a guarded headless owner-only source spike (this checkpoint).
2. G0 target/identity/rights/label review and G1 protected-source app-only/owner-delegation access experiment. No tenant mutation/consent is implied by preparing code.
3. G2 owner/A/B/C Office tests, own-copy edit/save, saved-copy preservation and crossed-copy denial, with output hashes and separate client caches. The candidate `VIEW/DOCEDIT/EDIT` set is not a proven Office Editor profile; built-in Editor is not assumed equivalent.
4. T07/T22 owner/portal ACL APIs/forms, optimistic concurrency and transactional audit using the existing SQL base. Reuse React list/tree/details; folders are local authorized metadata projections only.
5. T23/T24/T25 protected intake: quarantine encrypted bytes first; validate source hash/identity/label/protection; prove authorized content inspection, trusted malware scanning and bounded unprotected OOXML validation within a supervised private boundary. Bind encrypted source hash, decrypted inspection hash and the operation/evidence that relates them. Scanning only the encrypted envelope is not sufficient Clean evidence for content. Unsupported encryption, missing rights, unknown owner/grants, remediated or indeterminate scan all fail closed. No source promoted or published until this revised G6 passes.
6. T08/T09/T10/T11 fenced per-recipient execution, independent output evidence and exact immutable Blob-version release, cleanup/recovery; then full product flows/G3–G7. No reinterpretation of old tests as new acceptance.

The existing intake parser/seed generator and release snapshot implementation are legacy unprotected-source/report foundations. Keep them fail-closed: do not simply allow protected input into ZIP parsing or mark it Eligible. The current production BFF still reports unconfigured dependencies and releases no artifact. SQL's Ready prohibition remains until verified publication is implemented.

## Headless candidate now implemented

`MipFeasibility owner-only-preflight <private-config>` verifies configuration/acknowledgments and exact byte hash/size, holds input read-only, and exits before certificate lookup/MIP initialization/authentication. It does **not** detect encryption, parse XLSX, scan, verify ownership or pass a gate. Its success output explicitly says LOCAL_CONFIG_ONLY and NOT_RUN for authentication/inspection. A test deliberately uses non-workbook bytes to enforce that distinction.

`owner-only-run` is a separate live command that must only be invoked following explicit scoped approval. Candidate:

- Existing certificate app-only auth and strict endpoint/resource/consent allowlists; File SDK 1.18.124, MSAL 4.83.3, .NET 10. No SDK upgrade/new tenant permissions were applied.
- An engine explicitly delegated to the original RMS owner, with the service principal as discovery identity. This is service application authentication with MIP owner delegation, not browser delegated OAuth and not ownership delegated to the downloader. Assigning `DelegatedUserEmail` requests a context; it never grants that permission.
- Source must be observed protected with the expected UDP label/owner, a readable rights descriptor and no non-owner user/role grants. Unknown descriptor rejects. Empty explicit UserRights is treated as an implicit-owner candidate, requiring independent owner/client verification; privileged recovery identities are not enumerated by this descriptor.
- Before reprotection, check the effective handler's OWNER or EXPORT usage right. New handler and new recipient descriptor for each copy; same source label ID, retained original owner via explicit protection settings. No `RemoveProtection`, decrypted temporary-file/stream method, source overwrite, template mutation or broad grant is used in this spike. Internal SDK processing still requires a private trusted host/run root; no guarantee of zero native memory/temporary material is made.
- Read each committed output through a fresh handler; require protection, same label, original owner and exactly one ordinary requester with the exact candidate rights. Observed ContentIds must be nonempty and distinct across source/A/B; the tool does not set/reuse a ContentId. This checks identifiers, not cryptographic key independence or client cache behavior. Re-hash source under the read lock. Output candidates remain private local spike files, not published/downloadable artifacts.
- Matching results return exit 3/PARTIAL_NOT_PASSED. Errors return 2. G1/G2/G6 are not passed; no Blob binding, AV/content validation or Office evidence is produced by this command. Fresh handler readback is independent of requested descriptor construction, but not a different vendor implementation or a client denial test.

Config is strict (unknown JSON fields rejected), private and hash-pinned. The example defaults cannot run. Original owner and requester mapping/authorization must be verified by the operator; the config is not an HTTP input or a general authority to act as arbitrary users. Private run-root ACL, filesystem ancestor/reparse trust, SDK runtime/cache location, process supervision and cleanup are operator prerequisites. The candidate must stop on unavailable rights/readback; do not grant SuperUser as an automatic workaround.

## Sources and limitations

- [MIP delegation](https://learn.microsoft.com/en-us/information-protection/develop/concept-delegation): engine delegation affects policy/consumption/protection; handler-only delegation changes protection owner while service consumption remains separate.
- [User-defined permissions](https://learn.microsoft.com/en-us/information-protection/develop/concept-user-defined-permissions): per-file UserRights/SetProtection/Commit; not global label-recipient edits.
- [API permissions](https://learn.microsoft.com/en-us/information-protection/develop/concept-api-permissions): candidate application permissions to review are Content.DelegatedReader, Content.DelegatedWriter and UnifiedPolicy.Tenant.Read, all requiring admin consent. Exact grants/resource IDs/challenges must be confirmed for the chosen target; none granted here. Content.SuperUser is not the default solution.
- [File handlers](https://learn.microsoft.com/en-us/information-protection/develop/concept-handler-file-cpp): protection access checks and committed new files. Installed managed XML/signatures confirm the APIs compile; runtime semantics/rights/client access require live tests.

See owner-only-spike-runbook.md for the review packet. No automatic gate completion, SDK diagnostic success, label-only metadata or animation is security acceptance.
