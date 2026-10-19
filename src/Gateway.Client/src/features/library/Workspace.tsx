import { useMemo, useState } from 'react';
import type { FolderNode, LibraryDocument } from './model';
import { filterDocuments, folderTrail, projectFolders } from './model';
import { FolderNavigation } from './FolderNavigation';

export function Workspace({ documents, folders, simulation = false, requestCopy }: {
  documents: LibraryDocument[]; folders?: FolderNode[]; simulation?: boolean; requestCopy: (document: LibraryDocument) => Promise<void>;
}) {
  const [query, setQuery] = useState(''); const [owner, setOwner] = useState('');
  const [selection, setSelection] = useState<string | null>(null);
  const [folderId, setFolderId] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [pending, setPending] = useState(false);
  const directory = useMemo(() => projectFolders(folders ?? [{ id: 'gateway-library', name: 'Gateway library', documentIds: documents.map(d => d.id) }], filterDocuments(documents, '', owner)), [folders, documents, owner]);
  const trail = folderTrail(directory, folderId);
  const currentFolder = trail.at(-1);
  const searching = query.trim().length > 0;
  const inFolder = currentFolder && !searching ? documents.filter(d => currentFolder.documentIds.includes(d.id)) : documents;
  const visible = filterDocuments(inFolder, query, owner);
  function selectFolder(id: string | null) { setFolderId(id); setSelection(null); setQuery(''); setMessage(''); }
  const selected = visible.find(d => d.id === selection);
  return <>
    <div className="page-heading"><div><p className="eyebrow">YOUR DOCUMENT WORKSPACE</p><h1>Document library</h1>
      <p>Find a document. Request a copy issued only to you.</p></div></div>
    {simulation && <p className="notice">◈ Synthetic workspace preview · Fixture metadata only. No authentication, scan, protection or download.</p>}
    <div className="toolbar"><label className="search">Search documents<input type="search" placeholder="Search by title or filename" value={query} onChange={e => setQuery(e.target.value)} /></label>
      <label>Business owner<select value={owner} onChange={e => setOwner(e.target.value)}><option value="">All authorized owners</option>
        {[...new Set(documents.map(d => d.owner))].map(o => <option key={o}>{o}</option>)}</select></label>
      <span className="result-count">{visible.length} visible {visible.length === 1 ? 'document' : 'documents'}</span></div>
    <div className="workspace-grid with-folders"><FolderNavigation folders={directory} selectedId={searching ? null : currentFolder?.id ?? null} onSelect={selectFolder}/><section className="collection" aria-label="Authorized documents">
      <nav className="breadcrumbs" aria-label="Folder path"><button onClick={() => selectFolder(null)} aria-current={!currentFolder || searching ? 'location' : undefined}>All documents</button>
        {!searching && trail.map((folder, index) => <span key={folder.id}><span aria-hidden="true">/</span><button aria-current={index === trail.length - 1 ? 'location' : undefined} onClick={() => selectFolder(folder.id)}>{folder.name}</button></span>)}</nav>
      <div className="list-heading"><h2>{searching ? 'Search results' : currentFolder?.name ?? 'Documents'}</h2></div>
      <p className="list-context">{searching ? 'Searching all authorized folders.' : 'Select a file to see its owner, version and copy options.'}</p>
      {visible.length === 0 ? <div className="empty"><h3>No matching documents</h3><p>Try another search or owner. Only authorized documents appear here.</p></div> :
        <ul className="document-list">{visible.map(d => <li key={d.id}><button className={'document-row ' + (selected?.id === d.id ? 'selected' : '')}
          aria-pressed={selected?.id === d.id} onClick={() => setSelection(d.id)}><span className="file-icon" aria-hidden="true">X</span>
          <span className="document-name"><strong>{d.title}</strong><small>{d.owner} · {d.filename}</small></span>
          <span className="document-meta"><span>{d.classification}</span><small>{d.version} · {d.status}</small></span><span aria-hidden="true">↗</span></button></li>)}</ul>}
    </section><aside className="detail-panel" aria-label="Document details">
      {selected ? <><p className="eyebrow">SELECTED DOCUMENT</p><div className="large-file" aria-hidden="true">XLSX</div><h2>{selected.title}</h2><p>{selected.purpose}</p>
        <dl><dt>Business owner</dt><dd>{selected.owner}</dd><dt>Current version</dt><dd>{selected.version}</dd><dt>Classification</dt><dd>{selected.classification}</dd>
          <dt>Intended capability</dt><dd>Edit and save</dd><dt>Copy recipient</dt><dd>You, after server verification</dd></dl>
        <div className="info-note">The business owner and the protection owner are separate. Sharing a copy does not grant another person permission to open it. Client behavior still requires validation.</div>
        {simulation ? <p className="notice">Fixture only · Copy requests are unavailable in this spike.</p> :
          <button className="primary" disabled={!selected.canRequestCopy || pending} onClick={async () => {
            setPending(true); setMessage(''); try { await requestCopy(selected); setMessage('Copy requested. Follow its status in My downloads.'); }
            catch { setMessage('Copy request failed. Refresh your session and retry.'); } finally { setPending(false); }
          }}>{pending ? 'Requesting…' : 'Request my copy'}</button>}
      </> : <div className="empty-detail"><div className="large-file" aria-hidden="true">↗</div><h2>A copy of your own</h2><p>Select a document to see its owner, purpose and copy options.</p></div>}
      <p role="status">{message}</p>
    </aside></div>
  </>;
}
