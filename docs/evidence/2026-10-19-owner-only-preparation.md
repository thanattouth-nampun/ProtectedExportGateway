# Standalone owner-only source preparation — 19 October 2026

Local implementation checkpoint under ADR-013; not M0/M1/live acceptance. The user approved standalone content intake and deferred SharePoint/OneDrive/other content connectors. Entra/MIP/Azure and both report/library modules remain.

## Implemented

- ADR-013, precedence notices across docs/01–15, updated AGENTS/README/START_HERE and a target/permission/cost/rollback review packet. Existing evidence retained as historical. Legacy unprotected-source runtime rejects protected uploads until the revised intake gates are implemented.
- Pure observed-source/output predicates with 18 added tests: unknown protection/grants/roles, additional source users, changed owner, accumulated recipients and excess/duplicate output rights reject. Constructed observations are unit fixtures, not actual SDK facts.
- `owner-only-preflight` and `owner-only-run` commands with strict private config example. Local preflight stops before certificate/SDK/network; it binds bytes/hash, not workbook/protection/owner validity. Five explicit CLI checks are integrated into verification.
- Live candidate code: certificate app-only auth, explicitly authorized original-owner engine delegation, owner-only input observation, effective OWNER/EXPORT check, fresh per-recipient descriptors, same label/original owner, fresh read-locked output inspection, observed rights/matches/hashes and distinct nonempty ContentIds. Source is held read-only and hash checked unchanged. Distinct IDs do not prove independent content keys/client denial. No plaintext/decryption endpoint or protection-removal API added.
- Removed the disconnected-source panel from active folder navigation. The same authorized local folder/search/selection/keyboard behavior remains. No connector was installed or called.

## Actual commands/results

| Check | Actual result |
|---|---|
| `scripts/verify.ps1` | Exit 0; locked restore/build/publish and asset assertions pass |
| .NET unit tests | 59 passed (41 existing + 18 observed-rule cases) |
| Real LocalDB SQL tests | 15 passed |
| HTTP/BFF tests | 18 passed |
| Frontend unit tests | 7 passed |
| Chromium tests | 8 passed; includes absence of future-source panel |
| Total test suites | **107 passed** |
| Offline CLI preflight checks | **5 passed**, separate from the 107 tests |
| `scripts/verify-protected-spike.ps1` after final live-candidate edits | Build succeeds with 0 warnings/errors; 5 checks pass |
| Metadata-only installed SDK reflection | Confirms managed delegation, SetProtection/ProtectionSettings and string ContentId signatures; no tenant call |
| Native DLL load | PASS, SDK 1.18.124; liveAuthentication=NOT_RUN |
| Final LocalDB teardown inspection | 0 test databases remain |

Preflight checks: pinned-byte config success even for explicitly non-workbook test bytes, hash mismatch, overlapping recipients, missing owner-delegation approval, and unknown JSON field rejection. The non-workbook fixture guards against misrepresenting preflight as content/protection evidence. No `owner-only-run` command or certificate lookup was invoked. A missing Protection namespace import was caught by compilation and corrected. Expected failing CLI cases initially left LASTEXITCODE=2 despite all assertions passing; the verification script now resets status only after all five assertions pass. No test failure was relabeled as security success.

Same-origin release JS: 341.57 kB (gzip 107.94 kB); CSS: 17.98 kB (gzip 4.84 kB). Bundle sizes are not load/performance certification. Desktop screenshot visually inspected; [desktop](2026-10-19-standalone-desktop.png) and [mobile](2026-10-19-standalone-mobile.png) remain synthetic UI evidence only.

## Implementation / simulation / live separation

Implementation: source rules, guarded experiment code, documentation and existing runtime UI adjustments. Simulation: constructed observed-grant fixtures and private config/byte fixtures; no downloadable artifact. Live evidence: **none added** for tenant, owner-only input access, labeling, recipients, rights, Office, scanner or Azure. Native load remains native-load evidence only.

G0: user supplied a tenant ID; stored only in ignored local target metadata and not live-verified. Verified domain, processor app, certificate reference, label configuration, owner/A/B/C mappings and scoped authorization remain missing/unverified. G1/G2/G6 protected-source paths are NOT_RUN. G3/G7 have local subsets only; G4/G5 are NOT_RUN. No gate or M0–M3 milestone is marked complete.

No app registration, permission grant/admin consent, SuperUser assignment, tenant/label change, scanner invocation/exclusion, Azure provisioning or deployment performed. No automatic Git commit/push in this checkpoint.

Next proving step: complete the non-secret target worksheet, review exact processor/delegation/label/fixture scope and obtain approval, then run the owner-only G1/G2 experiment. Protected content scan/OOXML validation must be redesigned and proven before production intake; old source/parser/release contracts cannot be reused as acceptance of encrypted input.
