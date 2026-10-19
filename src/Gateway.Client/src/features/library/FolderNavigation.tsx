import { useRef, useState, type KeyboardEvent } from 'react';
import type { FolderView } from './model';
import { Tree, Folder } from '../../components/magicui/file-tree';

export function FolderNavigation({ folders, selectedId, onSelect }: {
  folders: FolderView[]; selectedId: string | null; onSelect: (id: string | null) => void;
}) {
  const [expanded, setExpanded] = useState<Set<string>>(() => new Set(folders.flatMap(f => [f.id, ...f.children.map(c => c.id)])));
  const [focused, setFocused] = useState<string | null>(null);
  const refs = useRef(new Map<string, HTMLLIElement>());
  const search = useRef({ value: '', time: 0 });
  const visible: { node: FolderView; parent: string | null }[] = [];
  function flatten(nodes: FolderView[], parent: string | null) {
    for (const node of nodes) { visible.push({ node, parent }); if (expanded.has(node.id)) flatten(node.children, node.id); }
  }
  flatten(folders, null);
  const tabId = visible.some(v => v.node.id === focused) ? focused : visible.find(v => v.node.id === selectedId)?.node.id ?? visible[0]?.node.id;
  function focus(id: string) { setFocused(id); refs.current.get(id)?.focus(); }
  function toggle(id: string) { setExpanded(old => { const next = new Set(old); if (next.has(id)) next.delete(id); else next.add(id); return next; }); }
  function keyboard(event: KeyboardEvent<HTMLLIElement>, node: FolderView, parent: string | null) {
    event.stopPropagation(); const index = visible.findIndex(v => v.node.id === node.id);
    switch (event.key) {
      case 'ArrowDown': event.preventDefault(); if (index < visible.length - 1) focus(visible[index + 1].node.id); break;
      case 'ArrowUp': event.preventDefault(); if (index > 0) focus(visible[index - 1].node.id); break;
      case 'Home': event.preventDefault(); if (visible.length) focus(visible[0].node.id); break;
      case 'End': event.preventDefault(); if (visible.length) focus(visible[visible.length - 1].node.id); break;
      case 'ArrowRight': event.preventDefault(); if (node.children.length) { if (!expanded.has(node.id)) toggle(node.id); else focus(node.children[0].id); } break;
      case 'ArrowLeft': event.preventDefault(); if (node.children.length && expanded.has(node.id)) toggle(node.id); else if (parent) focus(parent); break;
      case 'Enter': case ' ': event.preventDefault(); onSelect(node.id); break;
      default:
        if (event.key.length === 1 && !event.ctrlKey && !event.altKey && !event.metaKey) {
          event.preventDefault(); const now = Date.now();
          search.current = { value: (now - search.current.time < 600 ? search.current.value : '') + event.key.toLocaleLowerCase(), time: now };
          const next = [...visible.slice(index + 1), ...visible.slice(0, index + 1)].find(v => v.node.name.toLocaleLowerCase().startsWith(search.current.value));
          if (next) focus(next.node.id);
        }
    }
  }
  function render(nodes: FolderView[], parent: string | null) {
    return nodes.map(node => <li key={node.id} role="treeitem" aria-label={node.name} aria-selected={selectedId === node.id}
      aria-expanded={node.children.length ? expanded.has(node.id) : undefined} tabIndex={tabId === node.id ? 0 : -1}
      ref={element => { if (element) refs.current.set(node.id, element); else refs.current.delete(node.id); }}
      onFocus={event => { if (event.target === event.currentTarget) setFocused(node.id); }} onKeyDown={event => keyboard(event, node, parent)}>
      <Folder value={node.id} element={node.name} count={node.documentIds.length}>
        {render(node.children, node.id)}
      </Folder>
    </li>);
  }
  return <details className="folder-panel" open><summary>Browse folders</summary><div className="folder-panel-body">
    <button className={'all-documents ' + (selectedId === null ? 'selected' : '')} aria-pressed={selectedId === null} onClick={() => onSelect(null)}><span aria-hidden="true">▦</span> All documents</button>
    {folders.length ? <Tree selectedId={selectedId ?? undefined} onSelect={id => { focus(id); onSelect(id); }} expandedItems={[...expanded]} onExpandedChange={ids => setExpanded(new Set(ids))}>{render(folders, null)}</Tree> : <p className="folder-help">Folders appear when authorized documents are available.</p>}
    <p className="folder-help">Only folders containing documents in this view are shown.</p>
  </div></details>;
}
