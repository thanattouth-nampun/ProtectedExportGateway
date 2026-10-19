import { describe, expect, it } from 'vitest';
import { filterDocuments, folderTrail, projectFolders, type LibraryDocument } from './model';
const docs: LibraryDocument[] = [
  { id:'a',title:'Plan',filename:'plan.xlsx',owner:'O1',purpose:'Test',version:'v1',classification:'Unknown',status:'Fixture',canRequestCopy:false },
  { id:'b',title:'Budget',filename:'finance.xlsx',owner:'O2',purpose:'Test',version:'v1',classification:'Unknown',status:'Fixture',canRequestCopy:false },
];
describe('authorized document list', () => {
  it('filters only supplied authorized records by owner and filename', () => {
    expect(filterDocuments(docs,' FINANCE ', 'O2').map(d => d.id)).toEqual(['b']);
    expect(filterDocuments(docs,'Plan','O2')).toEqual([]);
  });
  it('does not reconstruct hidden owners or metadata', () => {
    expect(filterDocuments([docs[0]], '', '').map(d => d.owner)).toEqual(['O1']);
    expect(filterDocuments([], '', '')).toEqual([]);
  });
  it('preserves the complete supplied page', () => {
    const rows = Array.from({length: 60}, (_,i) => ({...docs[0],id:`d${i}`}));
    expect(filterDocuments(rows,'','')).toHaveLength(60);
  });
});
describe('authorized folder projection', () => {
  const folders = [{ id:'root',name:'Library',children:[
    { id:'planning',name:'Planning',documentIds:['a'] },
    { id:'finance',name:'Finance',documentIds:['b','b'] },
    { id:'hidden',name:'Private folder',documentIds:['not-authorized'] },
  ] }];
  it('prunes unknown files and branches and derives counts only from supplied documents', () => {
    const tree = projectFolders(folders, docs);
    expect(tree[0].documentIds).toEqual(['a','b']);
    expect(tree[0].children.map(n=>n.name)).toEqual(['Planning','Finance']);
    expect(projectFolders(folders, [])).toEqual([]);
  });
  it('returns authorized breadcrumb ancestors and no path for unknown selection', () => {
    const tree = projectFolders(folders, [docs[0]]);
    expect(folderTrail(tree,'planning').map(n=>n.name)).toEqual(['Library','Planning']);
    expect(folderTrail(tree,'hidden')).toEqual([]);
  });
  it('fails closed for ambiguous IDs or cycles while leaving document list data untouched', () => {
    expect(projectFolders([{id:'x',name:'A',documentIds:['a']},{id:'x',name:'B',documentIds:['b']}],docs)).toEqual([]);
    const cyclic: import('./model').FolderNode = {id:'cycle',name:'Cycle',documentIds:['a']}; cyclic.children=[cyclic];
    expect(projectFolders([cyclic],docs)).toEqual([]);
    expect(filterDocuments(docs,'','')).toHaveLength(2);
  });
  it('bounds the directory projection without silently truncating file search', () => {
    const tooMany = Array.from({length:201},(_,i)=>({id:`f${i}`,name:`Folder ${i}`,documentIds:['a']}));
    expect(projectFolders(tooMany,docs)).toEqual([]);
    expect(filterDocuments(docs,'budget','')).toHaveLength(1);
  });
});
