# Headless MIP feasibility tool

This is a local spike, not a production worker. It uses File SDK 1.18.124 and MSAL 4.83.3 on Windows x64/.NET 10. Native DLL loading has been observed; tenant authentication/protection has not.

```powershell
dotnet run --project tools/MipFeasibility -- api
dotnet run --project tools/MipFeasibility -- native-check
dotnet run --project tools/MipFeasibility -- run <absolute-private-config-path>
```

`api` lists installed public managed signatures without tenant calls. `native-check` loads native SDK DLLs without creating a profile or obtaining a token. `run` reads operator configuration, selects a valid private-key certificate from CurrentUser/My, constructs a service principal discovery identity `clientId@verifiedTenantDomain`, uses certificate-backed client credentials and exact resource/authority allowlists, then applies summary/payroll/A/B profiles to fresh output files. It never calls a decrypt API, adds SuperUser, delegates ownership to A/B, modifies a global label or publishes an output.

Copy `config.example.json` outside source control into a private directory. Fill metadata locally. Never send certificate keys/passwords/tokens through chat. The run root must have an operator-provisioned private filesystem ACL and must be outside webroot/static assets; it contains protected outputs and SDK runtime material and is not automatically sanitized or securely erased. Unknown config values are blockers; do not invent recipient mappings or protection owner. The empty allowlists intentionally block network consent/authentication until the actual commercial-cloud endpoints/resources have been reviewed. Sovereign clouds are not implemented.

`OperatorAcknowledgedSdkNetworkAndDiagnostics` is acknowledgment of SDK network/diagnostic behavior by an authorized local operator, not tenant admin consent or a licensing claim. Even minimal diagnostics do not mean zero SDK traffic. Review the Microsoft SDK setup/terms/telemetry requirements before enabling this flag. No live run was attempted in the initial milestone.

Independent readback uses a **new handler over committed output**: `Label.Label.Id`, `FileHandler.IsProtected`, `Protection.ProtectionDescriptor.UserRights/UserRoles`, and `Protection.Owner`. No expected recipient collection is reused as an observation. A null ad-hoc descriptor or unavailable rights/owner fails matching. `IProtectionHandler.Rights` describes the inspecting identity's rights and is deliberately not used as the recipient-grant list. The tool records matches/aliases/hashes rather than raw addresses/label IDs. These managed API semantics are confirmed in installed XML/reflection; successful tenant observations remain untested.

Installed constant reflection confirmed `Rights.View=VIEW`, `Rights.DocumentEdit=DOCEDIT`, `Rights.Edit=EDIT`; excluded constants include `EXPORT`, `OWNER`, `EXTRACT`. This proves encoding only, not Office edit/save behavior or recipient isolation.

The UDP spike pre-applies a fresh custom descriptor, then the UDP label; exact sequence compatibility is a live test. `HasAdhocProtection` must be true. It uses Standard assignment for the business rule without privileged override. Protection inspection errors fail rather than requesting broader tenant permissions. The initial spike uses fresh in-memory profiles; warm-cache/refresh/fault cases and supervision are still pending.

Exit codes: `0` help/API/native-load success; `2` invalid configuration, native/auth/apply/inspect failure; `3` initial apply/inspect observations match but **G1 remains incomplete**. No command currently emits a complete G1 PASS. The serialized G1 record always lists pending negative/freshness/client/cloud checks. G2 requires isolated Office clients, exact hashes and rights-based crossed-copy denial for original and saved A/B copies.

References: [application permissions](https://learn.microsoft.com/en-us/information-protection/develop/concept-api-permissions), [UDP](https://learn.microsoft.com/en-us/information-protection/develop/concept-user-defined-permissions), [service-principal discovery sample](https://github.com/Azure-Samples/MipSDK-File-Dotnet-ServicePrincipalAuth/blob/main/mip-sdk-dotnet-file-SPA/Action.cs). The archived sample informs identity discovery only; its old ADAL/trace logging is not adopted.
