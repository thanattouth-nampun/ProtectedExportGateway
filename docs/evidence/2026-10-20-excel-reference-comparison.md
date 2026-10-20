# Excel Restricted Editor reference comparison — 20 October 2026

The user supplied the existing SDK candidate and an Excel-created Restricted Editor reference for the previously agreed compare/refine/reissue experiment. Both were hash-pinned and held read-only for File SDK inspection using the existing processor and original-owner delegation. No supplied file was modified. No tenant permissions/licenses/policies, web release or Azure resources changed.

## Observed differences

| Observation | Prior SDK copy | Supplied Excel reference |
|---|---|---|
| SHA-256 | C74ECBDCFC32EE7806432DC9C5F8CD34FA1E307532555966C6EACFCF24D95787 | C63CE2D08AB33270C04EF6383C33B1F5A8BA62DA0F3628E451BD410A5C5C1BA4 |
| RMS owner / descriptor owner | Original owner | Same original owner |
| Delegated original owner's OWNER AccessCheck | True | True |
| Recipient A rights | VIEW, DOCEDIT, EDIT, VIEWRIGHTSDATA, REPLY, REPLYALL, FORWARD | VIEW, DOCEDIT, EDIT |
| Explicit original-owner grant | Absent | OWNER |
| Label | Same pinned UDP label | Same pinned UDP label |
| Label assignment / protection type | Privileged / Custom | Privileged / Custom |
| UserRoles | Empty | Empty |

The original owner was not lost in the prior SDK candidate. Its actual RMS owner and effective OWNER right were observed even though no explicit owner entry existed in UserRights. The Excel reference additionally contains that explicit entry. This identifies a concrete descriptor difference; the precise Excel UI rendering rule is not established by metadata comparison alone.

The prior seven-right candidate was derived from a generic permission-level table and did not match this Excel-produced profile. The user's screenshot showed A under Custom, not Restricted Editor. Both files report SDK ProtectionType Custom, so that enum alone does not identify the Excel UI category or establish an error.

## Reference-matched new candidate

Added an opt-in `MatchExcelReferenceProfile` to the isolated single-recipient spike. It requests exactly A -> VIEW/DOCEDIT/EDIT plus original owner -> OWNER. The owner remains a distinct protection owner and explicit owner grant, never a second ordinary recipient. No other user is added. Legacy two-recipient/production release rules remain unchanged; the opt-in flag rejects use outside the single-recipient diagnostic mode.

New output SHA-256: `4187DFF807DF8B83DE3A43445FAED6088197CE9581E7D386BF38228D9AE0EC41`.

The native run returned exit 3/PARTIAL_NOT_PASSED with independent handler readback matching pinned label, original RMS owner, two exact grants, A-only ordinary access and a distinct ContentId. A separate read-only comparison command reopened the new output and reference: owner and label matched, A rights sets were identical, explicit OWNER grants were identical, UserRoles empty. Input/output hashes were pinned; original source hash was unchanged. Private output binding/config/raw comparison remain ignored. Sanitized observations are in the accompanying JSON.

## Verification and limits

- Build: 0 warnings/errors.
- Unit tests: **101 passed**, including missing/changed owner, excess rights, role grants and extra-recipient rejection for the reference profile.
- Offline preflight: **10 checks passed**, including profile isolation from legacy two-recipient mode.
- Read-only comparison: exit 0, filesUnchanged=true. A deliberately wrong hash rejected before certificate/native initialization (exit 2, INPUT_HASH_OR_SIZE_MISMATCH).
- New live candidate: exit 3, applyInspectMatched=true. The user subsequently supplied an Excel Permissions screenshot showing A under Restricted Editor and the original owner under Owner, and confirmed the result. This is user-provided UI evidence, not an automated Office test.

SDK inspection proves metadata equivalence for the inspected owner/label/user-grant fields; the subsequent user screenshot confirms the expected Excel permission categories for the reported new candidate. This does not prove every publishing-license property, A open/edit/save, saved-copy rights, cross-copy denial, G6 scan/structural validation or release binding. G1 remains partial; G2/G6 not passed. No Ready artifact or portal download.

The user accepted the simplified one-copy goal as complete and explicitly deferred recipient behavior testing. Preserve that scope: observed app-only issuance, independent grant/owner/label readback and user-confirmed Excel permission categories. Isolated A open/edit/save and crossed-copy tests remain deferred, not passed. The user requested committing and pushing this milestone.
