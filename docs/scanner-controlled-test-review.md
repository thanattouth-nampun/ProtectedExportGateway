# T24 controlled detection review — prepared 26 October 2026

Status: HUMAN APPROVED on 26 October 2026 for the exact one-file scope below. The authorized attempt stopped during approved-vendor-fixture-fetch with HttpRequestException (HRESULT80131500); no test ZIP was written and no EICAR native custom scan executed. The folder contained only its private config immediately afterward. The failure did not identify an HTTP status or prove Defender detection. No mirror, bypass or retry was attempted. Afterward the private acknowledgment was reset false and the already-used permit was recorded; the revised harness uses a create-only one-shot claim to prevent accidental repeat requests. This is not approval to disable protection or publish artifacts; future retries/changed targets need scope review consistent with the stop condition below.

## Target and exact operation

- Existing local Windows workstation and installed Microsoft-signed Defender executable at `C:\ProgramData\Microsoft\Windows Defender\Platform\4.18.26090.9-0\MpCmdRun.exe`, SHA-256 `C0C9670D63A8A7DE9C0EF5BA58231826B0ABEE03C2B3BA6F858114D96EA12315`. Recheck signature/hash before a run; stop on drift.
- Ordinary user only; the probe refuses an administrator token. No elevated helper, service installation, new identity, tenant grant, AV preference/exclusion change or Azure resource.
- A fresh private current-user/SYSTEM ACL root fixed to this repo's `.runtime/scanner-detection-control`. Never use Downloads, quarantine source objects, the user's other workbooks or a public/static root. Do not reset an existing root or broaden ACLs to make a test pass.
- One fixed HTTPS GET to `https://secure.eicar.org/eicar.com.zip`; no redirects/mirrors or arbitrary URLs. Read at most185 bytes and require exactly184. Validate one archive entry named eicar.com, exactly68 uncompressed bytes, and the pinned standard payload SHA-256 before writing a server-GUID `.zip`. No extracted test file is written and no archive content is executed.
- Existing scanner CLI with `Scenario=VendorEicarZip`, `SyntheticScanAcknowledged=true`, `ControlledDetectionAcknowledged=true` and a new explicit authorization reference. These operator config flags are guardrails, not a substitute for human authorization. Default config remains benign/false. Code: `tools/MipFeasibility/DefenderScannerProbe.cs`.
- Fixed `-Scan -ScanType 3 -File <generated-zip> -DisableRemediation`; 120-second native deadline, bounded output/termination waits, before/after native health and binary/source hashing when accessible. Always diagnostic, never ScanEvidence/trusted Clean/Publish. Only the generated vendor fixture is targeted.

## Effects and interpretation

The [EICAR vendor](https://www.eicar.org/download-anti-malware-testfile/) describes a non-malicious antivirus response test, including an official184-byte archive. Endpoint protection can alert/quarantine or block access as if malware were present. Enterprise monitoring/SOC may receive an alert, and detection/audit history can persist after local cleanup. This is why the exact target and fixture require environment-owner approval.

Keep real-time protection enabled. If fetching/writing/opening is blocked, stop and report the observed stage/HRESULT; do not infer a native custom-scan verdict from an I/O error, restore/allow the fixture, use an exclusion, weaken AV or retry alternate sources. If native scan actually executes, retain the exact bounded output and distinguish detection from generic exit2/error; exit0 alone remains insufficient. The source XLSX, private receipts and SQL eligibility are outside this control.

## Cost, stop and rollback

New Azure resource cost THB0; no license purchase, cloud change or external paid service. Outbound vendor download uses the existing connection. Stop on executable/version drift, unexpected host/tenant/security setting changes, stale/disabled health or any attempt to escalate the web/MIP worker.

No protection settings should require rollback. Retain sanitized observations. A file quarantined by Defender stays in its normal quarantine workflow; do not restore/allow it. For a leftover ordinary test file, remove only the known generated token after confirming no native operation is active and after review; no recursive deletion of computed/broad paths. Removing a file cannot undo prior endpoint/security-operation alerts or audit logs. Native CLI timeout cannot prove service cancellation: leave the private fixture isolated, block retry/release and investigate.

Runbook command after scoped approval and private-root/config preparation:

```powershell
# From the repository root; no execution of the test archive/content:
dotnet tools/MipFeasibility/bin/Release/net10.0/MipFeasibility.dll defender-scanner-probe <absolute-private-controlled-config>
```

Required acceptance is observed detection/rejection semantics plus negative/error/timeout/health/cleanup evidence, not merely creating an AV alert. This packet and its local tests do not complete T24/G6.
