import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { request } from '../../app/api';
import type { components } from '../../contracts/api';

type Document = components['schemas']['OwnerDocumentView'];
type Version = components['schemas']['QuarantineVersionView'];
export function QuarantineUploads({ document, allowed }: { document: Document; allowed: boolean }) {
  const client = useQueryClient();
  const [file, setFile] = useState<File | null>(null); const [ack, setAck] = useState(false);
  const [pending, setPending] = useState(false); const [message, setMessage] = useState('');
  const key = useRef(''); const controller = useRef<AbortController | null>(null);
  const fileInput = useRef<HTMLInputElement | null>(null);
  useEffect(() => () => { controller.current?.abort(); controller.current = null; }, []);
  const versions = useQuery({ queryKey: ['quarantine-versions', document.id], enabled: allowed,
    queryFn: ({ signal }) => request<Version[]>(`/api/documents/${document.id}/uploads`, { signal }) });
  async function refresh() {
    await Promise.all([versions.refetch(), client.invalidateQueries({ queryKey: ['owner-document', document.id] }),
      client.invalidateQueries({ queryKey: ['owner-documents'] }), client.invalidateQueries({ queryKey: ['owner-audit', document.id] })]);
  }
  async function upload(event: React.FormEvent) {
    event.preventDefault(); if (!file || !ack || pending) return;
    if (file.size < 1 || file.size > 10 * 1024 * 1024 || !file.name.toLowerCase().endsWith('.xlsx') || file.name.length > 200) {
      setMessage('Choose a synthetic XLSX file between 1 byte and 10 MiB.'); return;
    }
    const current = new AbortController(); controller.current = current;
    const timeout = window.setTimeout(() => current.abort(), 120000);
    setPending(true); setMessage('');
    try {
      const { token } = await request<{ token: string }>('/auth/csrf', { signal: current.signal });
      const result = await request<Version>(`/api/documents/${document.id}/uploads`, { method: 'POST', signal: current.signal,
        headers: { 'Content-Type': 'application/octet-stream', 'X-CSRF-TOKEN': token, 'If-Match': document.eTag,
          'Idempotency-Key': key.current, 'X-Upload-Filename': encodeURIComponent(file.name), 'X-Synthetic-Fixture': 'true' }, body: file });
      if (!current.signal.aborted) {
        setMessage(`File received: ${result.state}. Ownership, malware scan and XLSX validation are not verified.`);
        key.current = ''; setFile(null); setAck(false); if (fileInput.current) fileInput.current.value = ''; await refresh();
      }
    } catch {
      if (controller.current === current) setMessage('Upload could not be confirmed. Refresh status before retrying. Retry keeps the same upload key for the selected file.');
    } finally { window.clearTimeout(timeout); if (controller.current === current) setPending(false); }
  }
  if (!allowed) return <section><h2>File versions</h2><p>Quarantine upload is not configured. Publishing remains blocked.</p></section>;
  return <section aria-label="Quarantine file versions"><h2>Local quarantine intake</h2>
    <p>This Development-only spike stores synthetic files privately for inspection. It does not prove RMS ownership, protection, malware safety or XLSX validity.</p>
    <form onSubmit={event => void upload(event)} aria-label="Upload synthetic workbook">
      <fieldset disabled={pending || document.status !== 'Draft'}><legend>Synthetic XLSX only · maximum 10 MiB</legend>
        <label htmlFor={`workbook-${document.id}`}>Workbook file</label>
        <input ref={fileInput} id={`workbook-${document.id}`} type="file" accept=".xlsx" required onChange={event => {
          setFile(event.target.files?.[0] ?? null); key.current = crypto.randomUUID(); setMessage('');
        }}/>
        <label className="quarantine-ack"><input type="checkbox" checked={ack} onChange={event => setAck(event.target.checked)}/>
          I confirm this workbook contains synthetic data only.</label>
        <button className="primary" type="submit" disabled={!file || !ack}>{pending ? 'Uploading…' : 'Upload to local quarantine'}</button>
      </fieldset>
    </form>
    {message && <p role="status">{message}</p>}
    <button type="button" disabled={pending} onClick={() => void refresh()}>Refresh upload status</button>
    {versions.isPending ? <p role="status">Loading file versions…</p> : versions.isError ? <p role="alert">File versions are unavailable. Refresh status before uploading again.</p> :
      versions.data.length ? <ul>{versions.data.map(version => <li key={version.id}><strong>{version.filename}</strong>
        <p>{version.state} · {version.bytes == null ? 'Size unknown' : `${Number(version.bytes)} bytes`}</p>
        {version.sha256 && <p className="quarantine-hash">SHA-256: {version.sha256}</p>}
        {version.errorCode && <p>Upload status: {version.errorCode}</p>}
        <p>Ownership, content scan and structural validation: not verified. Publish blocked.</p></li>)}</ul> : <p>No file versions received.</p>}
  </section>;
}
