import type { components } from '../../contracts/api';
export type LibraryDocument = components['schemas']['DocumentDto'];
export type LibraryPage = components['schemas']['DocumentPageDto'];
// UI projection only. Future folder metadata must come from an authorized server contract.
// This is independent from ExportJob.SourceKind and confers no copy/publish permission.
export type FolderNode = { id: string; name: string; documentIds?: string[]; children?: FolderNode[] };
export type FolderView = { id: string; name: string; documentIds: string[]; children: FolderView[] };
export function projectFolders(folders: FolderNode[], documents: LibraryDocument[]): FolderView[] {
  const authorized = new Set(documents.map(d => d.id));
  const seen = new Set<string>(); let invalid = false;
  function visit(nodes: FolderNode[], depth: number): FolderView[] {
    if (depth > 12) { invalid = true; return []; }
    return nodes.flatMap(node => {
      if (!node.id || seen.has(node.id) || seen.size >= 200) { invalid = true; return []; }
      seen.add(node.id);
      const children = visit(node.children ?? [], depth + 1);
      const documentIds = [...new Set([...(node.documentIds ?? []).filter(id => authorized.has(id)), ...children.flatMap(c => c.documentIds)])];
      return documentIds.length ? [{ id: node.id, name: node.name, documentIds, children }] : [];
    });
  }
  const result = visit(folders, 0);
  return invalid ? [] : result;
}
export function folderTrail(folders: FolderView[], id: string | null): FolderView[] {
  if (id === null) return [];
  for (const folder of folders) {
    if (folder.id === id) return [folder];
    const children = folderTrail(folder.children, id);
    if (children.length) return [folder, ...children];
  }
  return [];
}
export function filterDocuments(documents: LibraryDocument[], query: string, owner: string): LibraryDocument[] {
  const search = query.trim().toLocaleLowerCase();
  return documents.filter(d => (!owner || d.owner === owner)
    && `${d.title} ${d.filename}`.toLocaleLowerCase().includes(search));
}
