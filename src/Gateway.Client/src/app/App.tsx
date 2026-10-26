import { useEffect, useState } from 'react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { Link, NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { request, mutate } from './api';
import { Workspace } from '../features/library/Workspace';
import type { LibraryPage } from '../features/library/model';
import type { components } from '../contracts/api';
import { OwnerDocuments } from '../features/owner/OwnerDocuments';

type Session = components['schemas']['SessionDto'];
export function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [checking, setChecking] = useState(true); const [error, setError] = useState('');
  useEffect(() => {
    let controller = new AbortController(); let alive = true; let epoch = 0;
    const ended = () => { epoch++; controller.abort(); setSession(null); setError('Your session ended. Sign in again.'); };
    async function refresh() {
      controller.abort(); controller = new AbortController(); const currentController = controller; const requestEpoch = ++epoch;
      try {
        const current = await request<Session>('/auth/session', { signal: currentController.signal });
        if (alive && requestEpoch === epoch) { setSession(current); setError(''); }
      } catch { if (alive && !currentController.signal.aborted && requestEpoch === epoch) { setSession(null); setError('Sign in to access your authorized workspace.'); } }
      finally { if (alive && requestEpoch === epoch || alive && currentController.signal.aborted) setChecking(false); }
    }
    void refresh(); const timer = setInterval(() => void refresh(), 15000);
    window.addEventListener('focus', refresh); window.addEventListener('gateway:session-ended', ended);
    return () => { alive = false; controller.abort(); clearInterval(timer); window.removeEventListener('focus', refresh); window.removeEventListener('gateway:session-ended', ended); };
  }, []);
  if (checking) return <main className="sign-in"><p role="status">Checking your session…</p></main>;
  if (!session) return <main className="sign-in"><p className="eyebrow">PROTECTED EXPORT GATEWAY</p><h1>A copy of your own.</h1>
    <p>{error}</p><p>Use your organization's Microsoft Entra account to continue.</p><a className="primary button-link" href="/auth/login?returnUrl=/library">Sign in with Microsoft</a>
    <div className="info-note">Publishing and downloads are not available yet.</div></main>;
  const identity = JSON.stringify([session.tenantId, session.objectId, [...session.roles].sort(), session.canManageOwnDocuments, session.canUploadToLocalQuarantine]);
  return <AuthorizedApp key={identity} session={session} clearSession={() => setSession(null)}/>;
}
function AuthorizedApp({ session, clearSession }: { session: Session; clearSession: () => void }) {
  const [client] = useState(() => new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: 0, gcTime: 0 } } }));
  useEffect(() => () => { void client.cancelQueries(); client.clear(); }, [client]);
  async function logout() {
    await client.cancelQueries(); client.clear(); clearSession();
    try { const { token } = await request<{ token: string }>('/auth/csrf');
      const form = document.createElement('form'); form.method = 'post'; form.action = '/auth/logout';
      const field = document.createElement('input'); field.type = 'hidden'; field.name = '__RequestVerificationToken'; field.value = token;
      form.append(field); document.body.append(form); form.submit();
    } catch { window.location.assign('/sign-in'); }
  }
  return <QueryClientProvider client={client}><div className="app-shell"><a className="skip-link" href="#main">Skip to content</a>
    <aside className="sidebar"><Link className="brand" to="/library"><span className="brand-symbol">▱</span><span>Protected Export<small>GATEWAY</small></span></Link>
      <p className="nav-label">WORKSPACE</p><nav aria-label="Main navigation">
        {(session.canManageOwnDocuments || session.roles.includes('LibraryReader')) && <NavLink to="/library">▦ <span>Shared with me</span></NavLink>}
        {session.canManageOwnDocuments && <NavLink to="/my-documents">▤ <span>My documents</span></NavLink>}
        {(session.roles.includes('ReportReader') || session.roles.includes('PayrollExporter')) && <NavLink to="/reports">▥ <span>Reports</span></NavLink>}
        <NavLink to="/my-exports">↓ <span>My downloads</span></NavLink>
        {session.roles.includes('ExportAuditor') && <NavLink to="/audit">◷ <span>Audit log</span></NavLink>}
      </nav><div className="sidebar-note"><span>◈ {session.mode}</span><p>Downloads require verified live output.</p></div></aside>
    <div className="app-body"><header className="account-header"><span>Secure document workspace</span><div><span>{session.displayName}</span><button onClick={() => void logout()}>Sign out</button></div></header>
      <main id="main"><Routes><Route path="/library" element={<Library/>}/><Route path="/" element={<Navigate to="/library" replace/>}/>
        <Route path="/my-documents" element={<OwnerDocuments allowed={session.canManageOwnDocuments} uploadAllowed={session.canUploadToLocalQuarantine}/>}/>
        <Route path="/reports" element={<Pending title="Reports" description="Synthetic headcount and payroll exports will use server-owned classification rules. Live protection and download are blocked until the SDK gates pass."/>}/>
        <Route path="/my-exports" element={<Pending title="My downloads" description="Durable job history is not connected yet. Simulated processing does not produce a downloadable artifact."/>}/>
        <Route path="/audit" element={<Pending title="Audit log" description="The transactional metadata journal is not connected yet. Auditor access will not grant file download permissions."/>}/>
        <Route path="*" element={<Pending title="Page not found" description="Choose a workspace page from the navigation."/>}/></Routes></main>
    </div></div></QueryClientProvider>;
}
function Library() {
  const query = useQuery({ queryKey: ['documents'], queryFn: ({ signal }) => request<LibraryPage>('/api/documents', { signal }) });
  if (query.isPending) return <p role="status">Loading authorized documents…</p>;
  if (query.isError) return <section><h1>Shared with me</h1><p role="alert">Shared documents are not available yet. No source or copy can be downloaded.</p><button onClick={() => void query.refetch()}>Retry</button></section>;
  return <Workspace documents={query.data.items} requestCopy={async d => { await mutate(`/api/documents/${encodeURIComponent(d.id)}/copies`, { sourceVersion: d.version }); }}/>;
}
function Pending({ title, description }: { title: string; description: string }) { return <section className="pending"><p className="eyebrow">IMPLEMENTATION IN PROGRESS</p><h1>{title}</h1><p>{description}</p></section>; }
