// Development-only entry. Not imported by production main.tsx or emitted by the release build.
import { createRoot } from 'react-dom/client';
import { Workspace } from './features/library/Workspace';
import type { FolderNode, LibraryDocument } from './features/library/model';
import './style.css';

const documents: LibraryDocument[] = [
  { id: 'fixture-plan', title: 'Project delivery plan', purpose: 'A synthetic working plan for the operations team.', owner: 'Owner O1', filename: 'project-plan.xlsx', version: 'fixture-v1', classification: 'UDP label · not validated', status: 'Fixture', canRequestCopy: false },
  { id: 'fixture-resources', title: 'Resource allocation', purpose: 'Synthetic resource planning. No real employee information.', owner: 'Owner O1', filename: 'resource-allocation.xlsx', version: 'fixture-v1', classification: 'UDP label · not validated', status: 'Fixture', canRequestCopy: false },
  { id: 'fixture-budget', title: 'Budget working file', purpose: 'A bounded synthetic budget workbook for the finance team.', owner: 'Owner O2', filename: 'budget-working.xlsx', version: 'fixture-v1', classification: 'UDP label · not validated', status: 'Fixture', canRequestCopy: false },
];
const folders: FolderNode[] = [{ id: 'gateway', name: 'Gateway library', children: [
  { id: 'operations', name: 'Operations', children: [
    { id: 'planning', name: 'Planning', documentIds: ['fixture-plan'] },
    { id: 'resources', name: 'Resources', documentIds: ['fixture-resources'] },
  ] },
  { id: 'finance', name: 'Finance', documentIds: ['fixture-budget'] },
] }];
createRoot(document.getElementById('root')!).render(<div className="spike-shell"><header className="account-header"><strong>Protected Export Gateway</strong><span>Development harness · no sign-in</span></header>
  <main><Workspace documents={documents} folders={folders} simulation requestCopy={async () => { throw new Error('SIMULATED_NO_RELEASE'); }}/></main></div>);
