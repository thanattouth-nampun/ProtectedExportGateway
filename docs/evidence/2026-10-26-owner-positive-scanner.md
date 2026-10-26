# Actual Owner-positive and scanner callability evidence — 26 October 2026

## Actual Entra/browser/SDK observation

Restarted the existing local same-origin Development web artifact on https://localhost:7188 with its existing config/certificate/SQL store. No republish, tenant/certificate/role/consent change or Azure deployment. The human completed original Owner login at Microsoft; no credentials/tokens were extracted. Created a separate Draft through the UI with no selected recipients:

- Document: 74d924f8-9c5c-4b15-b354-cf6a6f1684a7, title Owner quarantine verification — 2026-10-26.
- Authenticated owner/uploader oid: 33f03fd3-d0d1-44c7-a664-9acbe8171560, existing reviewed thanattouth@baht.net mapping.
- Quarantine version: 53f6d76e-d10a-44bc-8526-fd55ce9d6b47, actual user-supplied owner-only-synthetic.xlsx, 45,568 bytes.
- Encrypted hash: 24B7904375817C5AA033F6713D23ABE01BB52A79FD589138A0BE60463DA46235, matching the supplied fixture and stored envelope.

Fresh FileSDK1.18.124/MSAL4.84.2 app-only/original-owner-delegated inspection acquired tokens for the existing target tenant and allowlisted policy/RMS resources. Result **OWNER_ONLY_OBSERVED_NOT_ELIGIBLE**, CanPublish/GatePassed=false, scan/contentValidation NOT_RUN. Same-file B mismatch is separately recorded in quarantine-ownership evidence. Positive metadata match is not Office-client access testing or content acceptance.

Independent post-probe SQL read confirmed revision3/ACLrevision1, no published pointer and zero export jobs. UI shows the received version AwaitingValidation, source editing/Publish disabled and inspection not verified. This is accurate because the diagnostic writes no persisted eligibility/scan evidence. [Actual screenshot](2026-10-26-owner-quarantine-receipt.png).

## Actual ordinary-user native scanner observation

Verified installed binary Authenticode Valid, signer Microsoft Windows Publisher/Microsoft Corporation; version4.18.26090.9; executable SHA-256 C0C9670D63A8A7DE9C0EF5BA58231826B0ABEE03C2B3BA6F858114D96EA12315. Ran the fixed native custom-scan diagnostic with an ordinary non-admin token; an administrator token would be refused. Tool escalation only removed sandbox restrictions, not Windows UAC/runas elevation.

- RunId: 692ff9f8-c9ea-4e7b-9dc8-08a311cae6de; generated benign synthetic headcount workbook, 6,339 bytes.
- Before/after hash identical: B2EFE30870C5C2587310C78BBF687AC7021B3E6EEEF392A44E36E13F6C3129E2.
- Fixed custom scan with DisableRemediation: native exit0, no timeout/truncation, stderr empty. Private output reported scan start/finish and no threats for exactly the generated fixture.
- Tool exit3 / **INVOCATION_COMPLETED_UNVERIFIED**, cleanVerdict NOT_ESTABLISHED, workerElevated=false, CanPublish/GatePassed=false. Not a trusted Clean/production scanner proof.

Post-run Defender health: antivirus/realtime enabled, Normal mode, product4.18.26090.9, engine1.1.26090.9, signature1.461.209.0 at 2026-10-26 00:32:51 UTC. No AV setting/exclusion/definition-update command, helper installation, protected-content extraction or workbook parser execution. Native scan was performed on a newly generated unprotected fixture, not on ciphertext or decrypted source. No EICAR fixture downloaded/created. Benign native scan ran before the no-behavior-change extraction of path/argument policies into unit-tested shared methods; final builds and policy tests passed.

## Actual verification this increment

- Release tool/unit builds: zero warnings/errors, serial MSBuild/disabled build servers.
- Unit tests: **124 passed, zero skips**, including 15 new scanner decision/path/argument cases. Pure decision tests do not prove native timeout/detection behavior.
- `scripts/verify-protected-spike.ps1`: **16 offline CLI checks passed**; 3 new scanner guards reject missing acknowledgment, unknown fields and missing executable hash before native invocation. The script never scans or authenticates to MIP.
- No frontend/API/SQL implementation changed this increment; their prior suites are not counted as fresh tests. Actual browser upload and read-only SQL/SDK observations above are separate from test simulations.

G1 partial; G2 deferred/not passed; **G6 not passed**. Current milestone proves actual owner-positive metadata matching and ordinary-user benign scan callability only. Native failure/detection controls, trusted result adapter, durable worker/evidence states, bounded protected inspection/cleanup and immutable Azure publication remain. [Runbook and proposed controlled-test scope](../scanner-feasibility-checkpoint.md).
