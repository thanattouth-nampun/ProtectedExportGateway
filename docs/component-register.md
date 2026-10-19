# Dependency, component and asset review — local checkpoint

Current v4.2: ADR-012 removes Three/R3F/Drei and renderer-only transitive packages. Exact retained package versions/lockfiles remain tested local pins, not live G1/G7 certification. TypeScript5.9.3 satisfies the DTO generator peer range; Node24.16.0 supports the retained Vite build. Earlier renderer compatibility/benchmark records remain historical.

| Item | Origin/license | Decision and verification |
|---|---|---|
| React/DOM 19.3.1 | react.dev / facebook/react, MIT | One client UI; no SSR/Node production server |
| Router 7.18.4, Query 5.104.4 | remix-run/react-router, TanStack/query, MIT | Server session, authorized-cache lifecycle and request cancellation; principal-change browser test |
| RHF 7.90.0, resolvers 5.9.2, Zod 4.6.7 | official packages, MIT | Pinned for upcoming owner forms; not yet used to claim wizard delivery |
| Tailwind/Vite plugin 4.3.3 | tailwindlabs, MIT | Shared semantic CSS tokens; no unrelated theme copied |
| Original shell/list/details/alerts | Created in this repository | Semantic HTML; no catalog code copied; browser keyboard/reflow/fallback tests, not accessibility certification |
| Folder navigation | Adapted [Magic UI File Tree source](https://github.com/magicuidesign/magicui/blob/ec1cce6c4192c0aaac279dd7e53537ccd5c99d44/apps/www/registry/magicui/file-tree.tsx), MIT | Source vendored in components/magicui/file-tree.tsx; license shipped in public/licenses. Controlled state, semantic theme and keyboard adapter; no connector or accessibility certification |
| Radix Accordion 1.3.1 / lucide-react 1.59.0 | Official radix-ui/primitives and lucide-icons/lucide packages; MIT / ISC | Exact npm pins, React 19 peer compatibility checked; used by vendored Magic UI component. No catalog CLI or remote runtime code |
| shadcn/21st candidates | docs/14 shortlist | Deferred until actual form/dialog adoption; source/license/accessibility review required before copying |
| Inter WOFF2 400/500/600 | [Official Inter 4.1](https://github.com/rsms/inter/releases/tag/v4.1), SIL OFL 1.1 | `web/Inter-Regular.woff2`, `Inter-Medium.woff2`, `Inter-SemiBold.woff2`, original LICENSE.txt retained in public/fonts; self-hosted, swap/system fallback |
| .NET SDK 10.0.300 / ASP.NET 10.0.8 | Microsoft | Builds/tests; host `--info` SCM inventory restriction documented |
| EF Core SqlServer/Design + dotnet-ef 10.0.13 | Microsoft, MIT | .NET 10 target; exact NuGet/tool lock, SQL Server compatibility level 160 and real LocalDB migrations/concurrency tests; not Azure evidence |
| System.Security.Cryptography.Pkcs 10.0.13 | Microsoft, MIT | Direct patched pin replaces SqlClient's older vulnerable transitive 9.0.11; restore initially rejected NU1902/NU1903, no warning suppression |
| File SDK 1.18.124 | Microsoft NuGet, nuspec MIT plus bundled license/third-party notices/terms | Native load passed; application use/tenant/IPIA/license requirements remain operator review; not broad legal approval |
| MSAL 4.83.3 | Microsoft, MIT | Certificate auth candidate; never log auth challenge/token/secret |
| ClosedXML 0.105.0 | ClosedXML, MIT | Trusted synthetic generation only; value-only workbook integration tests; dependency licenses retained in package graph |
| OpenAPI ASP.NET 10.0.8 / Microsoft.OpenApi 2.7.5 | Microsoft, MIT | Direct patched 2.x pin replaces vulnerable transitive 2.0.0; [upstream advisory](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc) |
| Test tooling | xUnit2.9.3, Test SDK18.0.1, Vitest5.0.5, Playwright1.64.1 | Domain/HTTP/list/browser regression; no GPU renderer requirement |
| GitHub Actions | Official actions checkout/setup-node/setup-dotnet/upload-artifact | Exact tag commit SHAs resolved on 19 October 2026; test-only read permission; hosted run not yet performed |

Inter release archive SHA-256: `9883FDD4A49D4FB66BD8177BA6625EF9A64AA45899767DDE3D36AA425756B11E`.

| Asset | SHA-256 |
|---|---|
| Inter-Regular.woff2 | E06F6B1BC553AAEA4E4668023ED0AB0A147129C3107F511BC7D03D361B0AE085 |
| Inter-Medium.woff2 | 0FF3E94614E1493EB556314FD247AE6C4A85A7783B4CC86BE539940CF83F2A48 |
| Inter-SemiBold.woff2 | 5CB7103E4E605989AFEBC03D989C79201E54B21B5183DB33981F70DB9178A301 |

License statements above are package/source metadata checks, not legal certification. Preserve package/upstream notices on redistribution; public/internal release status remains an environment-owner decision. Re-run vulnerability/peer/build/browser checks on upgrades and periodically review native SDK/runtime compatibility and upstream advisories. Registry audit currently reports no known vulnerabilities after the OpenAPI fix; this is not a security audit of the application.
