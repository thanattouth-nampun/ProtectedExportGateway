import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ApiError, mutate, request } from '../../app/api';
import { QuarantineUploads } from './QuarantineUploads';
import type { components } from '../../contracts/api';

type Document = components['schemas']['OwnerDocumentView'];
type Page = components['schemas']['OwnerDocumentPage'];
type Option = components['schemas']['RequesterOptionDto'];
type Audit = components['schemas']['OwnerAuditView'];
const schema = z.object({ title: z.string().trim().min(1, 'Enter a title.').max(200),
  purpose: z.string().trim().min(1, 'Describe the document purpose.').max(1000), requesterObjectIds: z.array(z.string()).max(50, 'Choose up to 50 identities.') });
type Form = z.infer<typeof schema>;

export function OwnerDocuments({ allowed, uploadAllowed = false }: { allowed: boolean; uploadAllowed?: boolean }) {
  const [selected, setSelected] = useState<string | null>(null);
  const [skip, setSkip] = useState(0);
  const [savedNotice, setSavedNotice] = useState('');
  const query = useQuery({ queryKey: ['owner-documents', skip], enabled: allowed, refetchOnWindowFocus: false,
    queryFn: ({ signal }) => request<Page>(`/api/my/documents?skip=${skip}`, { signal }) });
  const options = useQuery({ queryKey: ['requester-options'], enabled: allowed,
    queryFn: ({ signal }) => request<Option[]>('/api/document-requester-options', { signal }) });
  if (!allowed) return <section><h1>My documents</h1><p role="alert">A verified internal member session is required. Sign in again or contact your administrator.</p></section>;
  return <section aria-labelledby="owner-heading"><div className="page-heading"><div><p className="eyebrow">OWNER WORKSPACE</p>
    <h1 id="owner-heading">My documents</h1><p>Prepare private drafts and choose who may request a protected copy.</p></div>
    <button className="primary" onClick={() => { setSelected(null); setSavedNotice(''); }}>New draft</button></div>
    <div className="info-note">Drafts are private. {uploadAllowed ? 'Local quarantine intake is a synthetic-only inspection spike.' : 'Upload is not configured.'} Publishing and downloads are not available yet. Saving an access list does not grant access to the source file.</div>
    {savedNotice && <p role="status">{savedNotice}</p>}
    {query.isPending || options.isPending ? <p role="status">Loading your workspace…</p> : query.isError || options.isError ?
      <div><p role="alert">Your document workspace is unavailable. Retry to load the latest saved state.</p><button onClick={() => { void query.refetch(); void options.refetch(); }}>Retry</button></div> :
      <div className="owner-grid"><aside aria-label="Your drafts"><h2>Your drafts <small>({query.data.total})</small></h2>
        {!query.data.items.length && <p>No drafts yet. Create your first draft.</p>}
        <ul className="document-list">{query.data.items.map(doc => <li key={doc.id}><button className={'document-row ' + (selected === doc.id ? 'selected' : '')}
          aria-pressed={selected === doc.id} onClick={() => { setSelected(doc.id); setSavedNotice(''); }}><span className="document-name"><strong>{doc.title}</strong><small>{doc.status} · Revision {doc.revision}</small></span></button></li>)}</ul>
        <div className="owner-pagination"><button disabled={skip === 0} onClick={() => { setSkip(Math.max(0, skip - 25)); setSelected(null); }}>Previous</button>
          <button disabled={skip + Number(query.data.pageSize) >= Number(query.data.total)} onClick={() => { setSkip(skip + 25); setSelected(null); }}>Next</button></div>
      </aside><OwnerSelection key={selected ?? 'new'} id={selected} uploadAllowed={uploadAllowed} options={options.data} saved={id => { setSelected(id); setSavedNotice('Draft saved.'); }}/></div>}
  </section>;
}
function OwnerSelection({ id, options, saved, uploadAllowed }: { id: string | null; options: Option[]; saved: (id: string) => void; uploadAllowed: boolean }) {
  const doc = useQuery({ queryKey: ['owner-document', id], enabled: id !== null, refetchOnWindowFocus: false,
    queryFn: ({ signal }) => request<Document>(`/api/documents/${id}`, { signal }) });
  const audit = useQuery({ queryKey: ['owner-audit', id], enabled: id !== null,
    queryFn: ({ signal }) => request<Audit[]>(`/api/documents/${id}/audit`, { signal }) });
  if (id && doc.isPending) return <p role="status">Loading draft…</p>;
  if (id && doc.isError) return <div><p role="alert">This draft could not be loaded.</p><button onClick={() => void doc.refetch()}>Retry draft</button></div>;
  return <div className="owner-editor"><DraftForm key={doc.data?.eTag ?? 'new'} doc={doc.data} options={options} saved={saved} reload={() => void doc.refetch()}/>
    {id && doc.data && <QuarantineUploads document={doc.data} allowed={uploadAllowed}/>}
    {id && <section aria-label="Draft history"><h2>Recent activity</h2>{audit.isPending ? <p role="status">Loading activity…</p> : audit.isError ? <p role="alert">Activity is unavailable.</p> :
      <ul>{audit.data.map((event, index) => <li key={index}>{activityLabel(event.eventType)}
        {' · Revision '}{event.documentRevision}{' · '}{new Date(event.at).toLocaleString('en-GB')}</li>)}</ul>}</section>}
  </div>;
}
function activityLabel(event: string) {
  const labels: Record<string, string> = { DocumentDraftCreated: 'Draft created', DocumentDraftAclUpdated: 'Access list updated',
    DocumentDraftMetadataUpdated: 'Draft details updated', QuarantineUploadStarted: 'Upload started', QuarantineUploadRetried: 'Upload retried',
    QuarantineUploadReceived: 'File received in quarantine', QuarantineUploadFailed: 'Upload failed' };
  return labels[event] ?? 'Document activity';
}
function DraftForm({ doc, options, saved, reload }: { doc?: Document; options: Option[]; saved: (id: string) => void; reload: () => void }) {
  const client = useQueryClient();
  const controller = useRef<AbortController | null>(null);
  useEffect(() => () => controller.current?.abort(), []);
  const [notice, setNotice] = useState(''); const [conflict, setConflict] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const form = useForm<Form>({ resolver: zodResolver(schema), defaultValues: { title: doc?.title ?? '', purpose: doc?.purpose ?? '', requesterObjectIds: doc?.requesterObjectIds ?? [] } });
  const editable = !doc || doc.status === 'Draft' && doc.sourceOwnershipStatus === 'NoSourceUploaded';
  async function save(values: Form) {
    controller.current?.abort(); const current = new AbortController(); controller.current = current;
    setNotice(''); setConflict(false); setUncertain(false);
    try {
      const result = await mutate<Document>(doc ? `/api/documents/${doc.id}` : '/api/documents', values, current.signal,
        doc ? { method: 'PATCH', etag: doc.eTag } : {});
      if (current.signal.aborted) return;
      await client.invalidateQueries({ queryKey: ['owner-documents'] });
      await client.invalidateQueries({ queryKey: ['owner-audit', result.id] });
      client.setQueryData(['owner-document', result.id], result);
      saved(result.id); setNotice('Draft saved.');
    } catch (error) {
      if (current.signal.aborted) return;
      if (error instanceof ApiError && error.status === 412) { setConflict(true); setNotice('This draft changed in another session. Reload it before saving again. Your changes have not been saved.'); }
      else {
        setUncertain(true);
        setNotice(error instanceof ApiError && error.status === 403 ? 'You no longer have permission to save this draft.' : 'The save could not be confirmed. Reload the workspace before trying again.');
      }
    }
  }
  return <form onSubmit={form.handleSubmit(save)} aria-label="Draft details" noValidate><h2>{doc ? 'Edit draft' : 'Create draft'}</h2>
    <p>The draft owner is your signed-in identity. Source ownership will be verified separately during upload.</p>
    {!editable && <p role="alert">This document requires source ownership verification before editing.</p>}
    <fieldset disabled={!editable || form.formState.isSubmitting}><legend>Document details and access</legend>
      <label htmlFor="draft-title">Title</label><input id="draft-title" maxLength={200} {...form.register('title')} aria-invalid={!!form.formState.errors.title} aria-describedby="draft-title-error"/>
      <span id="draft-title-error" role="alert">{form.formState.errors.title?.message}</span>
      <label htmlFor="draft-purpose">Purpose</label><textarea id="draft-purpose" maxLength={1000} rows={3} {...form.register('purpose')} aria-invalid={!!form.formState.errors.purpose} aria-describedby="draft-purpose-error"/>
      <span id="draft-purpose-error" role="alert">{form.formState.errors.purpose?.message}</span>
      <fieldset className="owner-access"><legend>Who may request a protected copy?</legend><p>Choose from the reviewed identities. No one is selected automatically.</p>
        {options.map(option => <label key={option.objectId}><input type="checkbox" value={option.objectId} {...form.register('requesterObjectIds')}/><span>{option.displayName}</span></label>)}
        {!options.length && <p>No requester identities are available.</p>}
      </fieldset><span role="alert">{form.formState.errors.requesterObjectIds?.message}</span><button className="primary" type="submit" disabled={conflict || uncertain}>{form.formState.isSubmitting ? 'Saving…' : 'Save draft'}</button>
    </fieldset>
    {notice && <p role={conflict || uncertain ? 'alert' : 'status'}>{notice}</p>}{conflict && <button type="button" onClick={reload}>Reload draft</button>}
    {uncertain && <button type="button" onClick={() => window.location.reload()}>Reload workspace</button>}
    <div className="owner-gates"><button type="button" disabled>Upload version</button><button type="button" disabled>Publish</button><p>A verified source is required before publication.</p></div>
  </form>;
}
