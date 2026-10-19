(() => {
  'use strict';
  const $ = (s, root = document) => root.querySelector(s);
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const people = {
    A:{name:'วิว',alias:'ผู้ใช้ A',initial:'V',reader:true,payroll:true},
    B:{name:'นิ้ง',alias:'ผู้ใช้ B',initial:'N',reader:true},
    O1:{name:'อร',alias:'Publisher + LibraryReader · Owner O1',initial:'O',publisher:true,reader:true},
    O2:{name:'เมย์',alias:'Publisher + LibraryReader · Owner O2',initial:'M',publisher:true,reader:true},
    AUD:{name:'ผู้ตรวจสอบ',alias:'Export Auditor',initial:'AU',auditor:true}
  };
  let persona='A', page='library', filter='', ownerFilter='all', wizardStep=0, draft=null, toastTimer;
  const docs=[
    {id:'d1',title:'แผนงานโครงการ',purpose:'แผนงานและรายการติดตามโครงการสำหรับทีมที่ได้รับอนุญาต',filename:'Project-plan.xlsx',owner:'O1',acl:['A','B'],status:'Published',published:1,versions:[{n:1,state:'Eligible',name:'Project-plan.xlsx',stage:3}],updated:'16 ต.ค. 2569'},
    {id:'d2',title:'งบประมาณการดำเนินงาน',purpose:'เอกสารงบประมาณจำลองสำหรับการทำงานร่วมกันในโครงการ',filename:'Operating-budget.xlsx',owner:'O2',acl:['A','B'],status:'Published',published:1,versions:[{n:1,state:'Eligible',name:'Operating-budget.xlsx',stage:3}],updated:'15 ต.ค. 2569'},
    {id:'d3',title:'แผนกำลังคนประจำปี',purpose:'ฉบับร่างสำหรับทบทวนภายใน',filename:'Workforce-plan.xlsx',owner:'O1',acl:['A'],status:'Draft',published:null,versions:[{n:1,state:'AwaitingValidation',name:'Workforce-plan.xlsx',stage:0}],updated:'16 ต.ค. 2569'}
  ];
  const jobs=[];
  const events=[{action:'Published · ตัวอย่าง',doc:'แผนงานโครงการ',actor:'อร · O1',result:'ข้อมูลจำลอง'},{action:'Published · ตัวอย่าง',doc:'งบประมาณการดำเนินงาน',actor:'เมย์ · O2',result:'ข้อมูลจำลอง'}];
  const labels={library:'คลังเอกสาร',mine:'เอกสารของฉัน',reports:'รายงาน',history:'คำขอของฉัน',audit:'บันทึกการทำงาน',upload:'เพิ่มเอกสาร',login:'เข้าสู่ระบบ'};
  const exportStages=['รอดำเนินการ','เตรียมไฟล์','กำหนดสิทธิ์','ตรวจสอบสำเนา','จัดเก็บสำเนา','พร้อมดูตัวอย่าง'];
  const intakeStages=['รอส่งตรวจ','กำลังสแกน','ตรวจรูปแบบไฟล์','พร้อมเผยแพร่'];
  function toast(message){$('#toast').textContent=message;$('#toast').classList.add('visible');clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('#toast').classList.remove('visible'),4500);}
  function badge(text,tone=''){return `<span class="badge ${tone}">${esc(text)}</span>`;}
  function title(eyebrow,name,description,action=''){return `<div class="page-title"><div><p class="eyebrow">${eyebrow}</p><h1>${name}</h1><p class="subtle">${description}</p></div>${action}</div>`;}
  function owner(id){return `<span class="owner-cell"><span class="owner-dot ${id==='O2'?'mint':''}">${esc(people[id].initial)}</span>${esc(people[id].name)} <span class="file-meta">${id}</span></span>`;}
  function fileCell(d,manage=false){return `<div class="doc-name"><span class="file-icon" aria-hidden="true">XLSX</span><div><button class="file-title" data-action="${manage?'manage':'detail'}" data-id="${d.id}">${esc(d.title)}</button><span class="file-meta">${esc(d.filename)}</span></div></div>`;}
  function currentPublished(d){return d.status==='Published'&&d.published!==null;}
  function eligibleLibrary(){return docs.filter(d=>people[persona].reader&&currentPublished(d)&&d.acl.includes(persona));}
  function nav(){
    const p=people[persona], items=[];
    if(p.reader)items.push(['library','▦','คลังเอกสาร']);
    if(p.publisher)items.push(['mine','▤','เอกสารของฉัน']);
    if(p.reader||p.publisher)items.push(['reports','▥','รายงาน'],['history','◷','คำขอของฉัน']);
    if(p.auditor)items.push(['audit','≡','บันทึกการทำงาน']);
    $('#nav').innerHTML=items.map(([id,symbol,label])=>`<button class="nav-item ${page===id||(page==='upload'&&id==='mine')?'active':''}" data-page="${id}" ${page===id?'aria-current="page"':''}><span class="nav-symbol" aria-hidden="true">${symbol}</span>${label}${id==='history'&&jobs.filter(j=>j.user===persona).length?`<span class="nav-count">${jobs.filter(j=>j.user===persona).length}</span>`:''}</button>`).join('');
    $('#account-name').textContent=p.name+' · '+(persona==='A'||persona==='B'?`ผู้ใช้ ${persona}`:persona);
    $('#account-role').textContent=p.alias;
    $('#account-avatar').textContent=p.initial;
    $('#breadcrumb-page').textContent=labels[page];
  }
  function setPage(next){page=next;filter='';ownerFilter='all';$('#sidebar').classList.remove('open');$('#menu-toggle').setAttribute('aria-expanded','false');render();}
  function render(){nav();const views={library:renderLibrary,mine:renderMine,reports:renderReports,history:renderHistory,audit:renderAudit,upload:renderUpload,login:renderLogin};(views[page]||renderLibrary)();}
  function renderLibrary(){
    $('#main').innerHTML=title('YOUR DOCUMENT LIBRARY','คลังเอกสาร','เอกสารที่คุณมีสิทธิ์ขอสำเนา พร้อมสิทธิ์เฉพาะคุณ')+`<section class="hero" aria-label="แนวคิดสำเนาเฉพาะผู้ใช้"><div class="hero-copy"><p class="eyebrow">ONE SOURCE. YOUR OWN COPY.</p><h2>เอกสารเดียวกัน<br>สำเนาที่เป็นของคุณ</h2><p>ขอสำเนาเพื่อเปิด แก้ไข และบันทึก<br>โดยสิทธิ์ของคุณไม่ปะปนกับผู้ดาวน์โหลดคนอื่น</p><small><span aria-hidden="true">▣</span> สิทธิ์ออกให้แต่ละสำเนา · ต้นฉบับคงเดิม</small></div><div class="hero-art">${$('#scene-template').innerHTML}<span class="scene-caption">ต้นฉบับ → ป้องกัน → สำเนาเฉพาะคน</span></div></section><div class="toolbar"><label class="search-box"><span aria-hidden="true">⌕</span><input id="search" type="search" aria-label="ค้นหาเอกสาร" placeholder="ค้นหาชื่อเอกสารหรือชื่อไฟล์"></label><label><span class="sr-text">เจ้าของเอกสาร</span><select id="owner-filter" aria-label="กรองเจ้าของเอกสาร"><option value="all">Owner ทั้งหมด</option><option value="O1">อร · O1</option><option value="O2">เมย์ · O2</option></select></label><span class="result-count" id="result-count"></span></div><section class="panel" aria-label="รายการเอกสาร"><div class="panel-head"><h2>เอกสารที่พร้อมให้ขอสำเนา</h2><span>XLSX · ตัวอย่าง</span></div><div id="library-rows"></div></section><p class="below-table"><span class="info" aria-hidden="true">i</span>Owner คือผู้รับผิดชอบเอกสาร · ผู้รับสิทธิ์ในสำเนาที่คุณขอคือคุณ</p>`;
    renderLibraryRows();
    $('#search').addEventListener('input',e=>{filter=e.target.value;renderLibraryRows();});
    $('#owner-filter').addEventListener('change',e=>{ownerFilter=e.target.value;renderLibraryRows();});
  }
  function renderLibraryRows(){
    const visible=eligibleLibrary().filter(d=>(ownerFilter==='all'||d.owner===ownerFilter)&&(d.title+' '+d.filename).toLowerCase().includes(filter.toLowerCase()));
    $('#result-count').textContent=`${visible.length} เอกสาร`;
    $('#library-rows').innerHTML=visible.length?`<table class="doc-table"><thead><tr><th scope="col">เอกสาร</th><th scope="col">Owner</th><th scope="col">Classification</th><th scope="col" class="updated-col">เวอร์ชัน</th><th scope="col">การทำงาน</th></tr></thead><tbody>${visible.map(d=>`<tr><td>${fileCell(d)}</td><td>${owner(d.owner)}</td><td>${badge('Confidential · Custom')}</td><td class="updated-col">v${d.published}<span class="file-meta">${d.updated}</span></td><td><button class="secondary row-action" data-action="detail" data-id="${d.id}">ขอสำเนาสำหรับฉัน <span aria-hidden="true">↗</span></button></td></tr>`).join('')}</tbody></table>`:`<div class="empty"><h2>ไม่พบเอกสารที่ตรงกับการค้นหา</h2><p>ลองเปลี่ยนคำค้นหรือ Owner ตัวกรองนี้ค้นเฉพาะรายการที่บัญชีตัวอย่างมีสิทธิ์</p></div>`;
  }
  function detail(d){
    if(!eligibleLibrary().some(x=>x.id===d.id)){toast('บัญชีตัวอย่างนี้ไม่มีสิทธิ์ขอสำเนาเอกสารนี้');return;}
    $('#detail-content').innerHTML=`<div class="dialog-head"><div><p class="eyebrow">DOCUMENT DETAILS</p><h2 id="detail-title">${esc(d.title)}</h2><p class="subtle">${esc(d.filename)} · v${d.published}</p></div><button class="dialog-close" data-close="detail" aria-label="ปิดรายละเอียด">×</button></div><div class="dialog-body">${badge('เผยแพร่แล้ว','green')}<p class="scope-note">${esc(d.purpose)}</p><dl class="summary-list"><div><dt>Owner ต้นฉบับ</dt><dd>${esc(people[d.owner].name)} · ${d.owner}</dd></div><div><dt>ผู้รับสำเนานี้</dt><dd>${esc(people[persona].name)} · ${persona} <strong>(คุณ)</strong></dd></div><div><dt>สิทธิ์ที่กำหนด</dt><dd>เปิด แก้ไข และบันทึก</dd></div><div><dt>Label ที่คาดหวัง</dt><dd>Confidential · Custom</dd></div></dl><div class="callout">แต่ละคนได้รับสำเนาของตนเอง การส่งไฟล์ต่อไม่ได้เพิ่มสิทธิ์ให้ผู้รับคนอื่นตามข้อกำหนด</div><p class="scope-note">ต้นแบบนี้แสดงแนวทาง UX เท่านั้น ยังไม่ได้ออกไฟล์หรือพิสูจน์สิทธิ์จริง</p></div><div class="dialog-actions"><button class="secondary" data-close="detail">ปิด</button><button class="primary" data-action="request" data-id="${d.id}">จำลองขอสำเนาสำหรับฉัน</button></div>`;
    openDetail();
  }
  function openDetail(){const dlg=$('#detail-dialog');if(!dlg.open)dlg.showModal();}
  function requestCopy(d){
    if(!eligibleLibrary().some(x=>x.id===d.id))return toast('ไม่สามารถขอสำเนาได้ในสถานะปัจจุบัน');
    const j={id:'EX-'+String(jobs.length+1).padStart(3,'0'),user:persona,title:d.title,doc:d.id,sourceVersion:d.published,stage:0};jobs.unshift(j);events.unshift({action:'Copy requested · จำลอง',doc:d.title,actor:people[persona].name,result:'จำลอง'});showJob(j);nav();
  }
  function showJob(j){
    if(j.user!==persona)return toast('คำขอนี้เป็นของบัญชีตัวอย่างอื่น');
    const withdrawn=j.doc&&(!currentPublished(docs.find(d=>d.id===j.doc))||docs.find(d=>d.id===j.doc).published!==j.sourceVersion);
    $('#detail-content').innerHTML=`<div class="dialog-head"><div><p class="eyebrow">MY COPY · ${j.id}</p><h2 id="detail-title">${esc(j.title)}</h2><p class="subtle">สำเนาสำหรับ ${esc(people[j.user].name)} · ข้อมูลจำลอง</p></div><button class="dialog-close" data-close="detail" aria-label="ปิดสถานะ">×</button></div><div class="dialog-body">${badge('UX simulation','amber')}${withdrawn?'<div class="callout warning">เอกสารถอนเผยแพร่หรือเปลี่ยนเวอร์ชันแล้ว คำขอนี้ไม่สามารถรับสำเนาผ่าน portal ได้</div>':`<ol class="status-track">${exportStages.map((label,i)=>`<li class="${i<j.stage?'done':i===j.stage?'current':''}"><b>${i<j.stage?'✓':i+1}</b>${label}${i===j.stage?' · จำลอง':''}</li>`).join('')}</ol>`}<div class="callout warning">ไม่มีการประมวลผล MIP หรือดาวน์โหลดไฟล์จริง ปุ่มถัดไปใช้สำรวจสถานะหน้าจอเท่านั้น</div></div><div class="dialog-actions"><button class="secondary" data-close="detail">ปิด</button>${withdrawn?'':j.stage<5?`<button class="primary" data-action="advance-job" data-id="${j.id}">ขั้นตอนถัดไป (จำลอง)</button>`:`<button class="primary" data-action="rights" data-id="${j.id}">ดูตัวอย่างสิทธิ์ของสำเนา</button>`}</div>`;openDetail();
  }
  function rights(j){
    if(!j.doc){$('#detail-content').innerHTML=`<div class="dialog-head"><div><p class="eyebrow">REPORT PROFILE · DESIGN INTENT</p><h2 id="detail-title">${esc(j.title)}</h2></div><button class="dialog-close" data-close="detail" aria-label="ปิด">×</button></div><div class="dialog-body">${badge('ข้อกำหนดจำลอง · ยังไม่ทดสอบ','amber')}<dl class="summary-list"><div><dt>Label ที่คาดหวัง</dt><dd>${j.title==='Payroll Detail'?'Confidential · Admin-defined':'Internal · Label only'}</dd></div><div><dt>การกำหนดสิทธิ์</dt><dd>${j.title==='Payroll Detail'?'ตามผู้รับที่ admin กำหนดใน report label':'ไม่มีการเข้ารหัสตาม report profile'}</dd></div></dl><p class="scope-note">Report profile แยกจาก per-user copies ใน Document Library ไม่มีการออกไฟล์หรือพิสูจน์ protection ในต้นแบบ</p></div><div class="dialog-actions"><button class="primary" data-close="detail">เข้าใจแล้ว</button></div>`;openDetail();return;}
    const other=j.user==='A'?'B':'A';
    $('#detail-content').innerHTML=`<div class="dialog-head"><div><p class="eyebrow">COPY RIGHTS · DESIGN INTENT</p><h2 id="detail-title">สิทธิ์เฉพาะสำเนาของคุณ</h2><p class="subtle">${esc(j.title)} · ${j.id}</p></div><button class="dialog-close" data-close="detail" aria-label="ปิดตัวอย่างสิทธิ์">×</button></div><div class="dialog-body">${badge('ตัวอย่างข้อกำหนด · ยังไม่ทดสอบ','amber')}<div class="rights-grid"><div class="rights-box"><strong>${esc(people[j.user].name)} · ${j.user}</strong><p>เปิด แก้ไข และบันทึก</p><small>ผู้รับสำเนานี้ตามข้อกำหนด</small></div><div class="rights-box denied"><strong>${esc(people[other].name)} · ${other}</strong><p>ไม่ได้รับสิทธิ์เปิดสำเนานี้</p><small>ต้องขอสำเนาของตนเองจากเว็บ</small></div></div><p class="scope-note">เปลี่ยนบทบาทจำลองด้านบนเพื่อดูคำขอของแต่ละคน ข้อมูลนี้ไม่ใช่ผลทดสอบ RMS หรือหน้าจอการปฏิเสธของ Excel</p><div class="callout">Label เดียวกันได้ · ต้นฉบับเดียวกันได้ · แต่ recipient และ artifact ของแต่ละคำขอแยกกัน</div><button class="secondary" disabled>ดาวน์โหลดไฟล์จริง — ไม่มีในต้นแบบ</button></div><div class="dialog-actions"><button class="primary" data-close="detail">เข้าใจแล้ว</button></div>`;openDetail();
  }
  function renderMine(){
    if(!people[persona].publisher)return setPage('library');
    const own=docs.filter(d=>d.owner===persona);
    $('#main').innerHTML=title('OWNER WORKSPACE','เอกสารของฉัน','จัดการรายละเอียด ผู้ขอสำเนา และเวอร์ชันที่เผยแพร่',`<button class="primary" data-action="new">＋ เพิ่มเอกสาร</button>`)+`<div class="callout">Owner มาจากบัญชีที่ล็อกอิน · การอัปโหลดใหม่ยังไม่แทนเวอร์ชันที่เผยแพร่ จนกว่าจะสั่งเผยแพร่เอง</div><section class="panel"><div class="panel-head"><h2>เอกสารที่คุณรับผิดชอบ</h2><span>${own.length} เอกสารตัวอย่าง</span></div>${own.length?`<table class="doc-table"><thead><tr><th>เอกสาร</th><th>สถานะเผยแพร่</th><th>เวอร์ชันล่าสุด</th><th>จัดการ</th></tr></thead><tbody>${own.map(d=>{const v=d.versions.at(-1);return `<tr><td>${fileCell(d,true)}</td><td>${badge(d.status==='Published'?`เผยแพร่ v${d.published}`:d.status==='Withdrawn'?'ถอนเผยแพร่':'ฉบับร่าง',d.status==='Published'?'green':'grey')}</td><td>v${v.n}<span class="file-meta">${v.state==='Eligible'?'พร้อมเผยแพร่ · จำลอง':v.state==='Rejected'?'ไม่ผ่าน · จำลอง':'กำลังตรวจ · จำลอง'}</span></td><td><button class="secondary row-action" data-action="manage" data-id="${d.id}">จัดการเอกสาร</button></td></tr>`;}).join('')}</tbody></table>`:'<div class="empty"><h2>เริ่มเอกสารแรกของคุณ</h2><p>เพิ่มไฟล์ รายละเอียด และรายชื่อผู้ที่ขอสำเนาได้</p><button class="primary" data-action="new">เพิ่มเอกสาร</button></div>'}</section>`;
  }
  function manage(d){
    if(!people[persona].publisher||d.owner!==persona)return toast('คุณจัดการได้เฉพาะเอกสารของตนเอง');
    const latest=d.versions.at(-1);
    $('#detail-content').innerHTML=`<div class="dialog-head"><div><p class="eyebrow">OWNER WORKSPACE · ${persona}</p><h2 id="detail-title">${esc(d.title)}</h2><p class="subtle">Owner ${esc(people[d.owner].name)} · สถานะทั้งหมดจำลอง</p></div><button class="dialog-close" data-close="detail" aria-label="ปิดจัดการเอกสาร">×</button></div><div class="dialog-body"><dl class="summary-list"><div><dt>เผยแพร่อยู่</dt><dd>${d.status==='Published'?`v${d.published}`:d.status==='Withdrawn'?'ถอนเผยแพร่แล้ว':'ยังไม่เผยแพร่'}</dd></div><div><dt>ผู้ขอสำเนา</dt><dd>${d.acl.map(id=>esc(people[id].name)+' · '+id).join(', ')||'ยังไม่มี'}</dd></div></dl><div class="action-row"><button class="secondary" data-action="new-version" data-id="${d.id}">＋ อัปโหลดเวอร์ชันใหม่</button><button class="secondary" data-action="edit-metadata" data-id="${d.id}">แก้รายละเอียด / ผู้ขอสำเนา</button></div><h3>ประวัติเวอร์ชัน</h3>${d.versions.slice().reverse().map(v=>`<section class="version-card"><div class="version-top"><strong>v${v.n} · ${esc(v.name)}</strong>${badge(v.state==='Eligible'?'พร้อมเผยแพร่':v.state==='Rejected'?'ไม่ผ่าน':'กำลังตรวจ',v.state==='Eligible'?'green':v.state==='Rejected'?'red':'amber')}</div><p>${v.state==='Eligible'?'ตัวอย่างผลหลังตรวจผ่าน · ไม่ใช่หลักฐาน scanner จริง':v.state==='Rejected'?'ตัวอย่างไฟล์ไม่ผ่าน ไม่มีการเผยแพร่':'ขั้นตอน: '+intakeStages[v.stage]+' · จำลอง'}</p>${v.state==='AwaitingValidation'?`<button class="secondary" data-action="advance-intake" data-id="${d.id}" data-version="${v.n}">จำลองขั้นตอนตรวจถัดไป</button><button class="quiet" data-action="reject-intake" data-id="${d.id}" data-version="${v.n}">จำลองตรวจไม่ผ่าน</button>`:''}${v.state==='Eligible'&&!(d.status==='Published'&&d.published===v.n)?`<button class="primary" data-action="publish" data-id="${d.id}" data-version="${v.n}">จำลองเผยแพร่ v${v.n}</button>`:''}</section>`).join('')}<p class="scope-note">ทุกการตรวจและการเผยแพร่ในหน้านี้เป็น fixture ของต้นแบบ ไม่มี live source ถูกเผยแพร่</p></div><div class="dialog-actions">${d.status==='Published'?`<button class="secondary" data-action="withdraw" data-id="${d.id}">ถอนเผยแพร่</button>`:''}<button class="primary" data-close="detail">ปิด</button></div>`;openDetail();
  }
  function startWizard(docId=null){
    const d=docs.find(x=>x.id===docId);if(!people[persona].publisher||(d&&d.owner!==persona))return;
    if($('#detail-dialog').open)$('#detail-dialog').close();
    wizardStep=0;draft={docId,title:d?.title||'',purpose:d?.purpose||'',acl:d?[...d.acl]:['A','B'],file:null,error:''};setPage('upload');
  }
  function renderUpload(){
    if(!people[persona].publisher||!draft)return setPage('mine');
    const steps=['ไฟล์และรายละเอียด','ผู้ขอสำเนา','ตรวจทานและส่งตรวจ'];
    const body=wizardStep===0?`<h2>เริ่มจากไฟล์และรายละเอียด</h2><p class="subtle">Owner คือ ${esc(people[persona].name)} · ${persona} จากบัญชีตัวอย่าง</p><div class="field"><label for="doc-title">ชื่อเอกสาร <span aria-hidden="true">*</span></label><input id="doc-title" type="text" maxlength="120" required value="${esc(draft.title)}" placeholder="เช่น แผนงานโครงการไตรมาส 4"></div><div class="field"><label for="doc-purpose">วัตถุประสงค์ <span aria-hidden="true">*</span></label><textarea id="doc-purpose" maxlength="500" required placeholder="เอกสารนี้ใช้สำหรับอะไร">${esc(draft.purpose)}</textarea></div><div class="field"><span class="field-label">ไฟล์ XLSX</span><div class="dropzone"><strong>เลือกไฟล์ หรือใช้ไฟล์ตัวอย่าง</strong><p>หนึ่งไฟล์ · ไม่เกิน 10 MiB · ยังไม่มี password หรือ MIP protection</p><label for="file-input" class="secondary">เลือกไฟล์จากเครื่อง</label><input id="file-input" class="file-input" type="file" accept=".xlsx" aria-describedby="upload-note"><button class="quiet" data-action="sample-file">ใช้ไฟล์ตัวอย่าง Project-plan.xlsx</button><small id="upload-note">ต้นแบบอ่านเพียงชื่อและขนาด ไม่อ่านเนื้อหาหรือส่งไฟล์ออกไป</small></div>${draft.file?`<div class="selected-file"><span>${esc(draft.file.name)}</span><span>${(draft.file.size/1024).toFixed(0)} KB · จำลอง</span></div>`:''}</div>`:wizardStep===1?`<h2>ใครขอสำเนาของตนเองได้บ้าง</h2><p class="subtle">เลือกรายชื่อจากบัญชีตัวอย่างในองค์กร</p><div class="callout">รายชื่อนี้ให้สิทธิ์ขอสำเนาผ่าน portal ไม่ได้ให้ทุกคนเปิดไฟล์สำเนาเดียวกัน</div>${['A','B',persona].map(id=>`<label class="check-option"><input type="checkbox" name="requester" value="${id}" ${draft.acl.includes(id)?'checked':''}><span><strong>${esc(people[id].name)} · ${id}</strong><small>${id===persona?'Owner ต้องมี LibraryReader ด้วยในระบบจริง':'ขอสำเนาเฉพาะตน · เปิด/แก้ไข/บันทึก'}</small></span></label>`).join('')}`:`<h2>ตรวจทานก่อนส่งตรวจ</h2><p class="subtle">การส่งไฟล์ยังไม่ใช่การเผยแพร่</p><dl class="summary-list"><div><dt>ชื่อเอกสาร</dt><dd>${esc(draft.title)}</dd></div><div><dt>วัตถุประสงค์</dt><dd>${esc(draft.purpose)}</dd></div><div><dt>Owner</dt><dd>${esc(people[persona].name)} · ${persona}</dd></div><div><dt>ไฟล์</dt><dd>${esc(draft.file?.name)}</dd></div><div><dt>ผู้ขอสำเนา</dt><dd>${draft.acl.map(id=>esc(people[id].name)+' · '+id).join(', ')||'ยังไม่มี'}</dd></div><div><dt>สิทธิ์ในสำเนา</dt><dd>เฉพาะผู้ขอ · เปิด/แก้ไข/บันทึก</dd></div></dl><div class="callout warning">ต้นแบบจำลองการส่งตรวจ ไม่มีการ upload หรือ scan จริง เมื่อเป็นระบบจริง Owner ต้องรอผลตรวจผ่านและกดเผยแพร่แยกต่างหาก</div>`;
    $('#main').innerHTML=title('OWNER WORKSPACE',draft.docId?'เพิ่มเวอร์ชันใหม่':'เพิ่มเอกสาร','Draft → ส่งตรวจ → พร้อมเผยแพร่ → Owner เผยแพร่')+`<section class="panel form-card"><div class="stepper" aria-label="ขั้นตอนเพิ่มเอกสาร">${steps.map((s,i)=>`<div class="step ${i===wizardStep?'active':i<wizardStep?'complete':''}" ${i===wizardStep?'aria-current="step"':''}><b>${i<wizardStep?'✓':i+1}</b>${s}</div>`).join('')}</div><div class="form-body">${draft.error?`<div class="field-error" role="alert">${esc(draft.error)}</div>`:''}${body}</div><div class="form-footer"><button class="secondary" data-action="${wizardStep?'wizard-back':'cancel-upload'}">${wizardStep?'ย้อนกลับ':'ยกเลิก'}</button><button class="primary" data-action="${wizardStep===2?'submit-upload':'wizard-next'}">${wizardStep===2?'จำลองส่งตรวจไฟล์':'ถัดไป →'}</button></div></section>`;
    if(wizardStep===0){$('#doc-title').addEventListener('input',e=>draft.title=e.target.value);$('#doc-purpose').addEventListener('input',e=>draft.purpose=e.target.value);$('#file-input').addEventListener('change',e=>{const f=e.target.files[0];if(!f)return;draft.error=!/\.xlsx$/i.test(f.name)?'รองรับเฉพาะไฟล์ .xlsx':f.size>10485760?'ไฟล์ต้องไม่เกิน 10 MiB':'';draft.file=draft.error?null:{name:f.name,size:f.size};renderUpload();});}
    if(wizardStep===1)document.querySelectorAll('[name=requester]').forEach(el=>el.addEventListener('change',()=>{draft.acl=Array.from(document.querySelectorAll('[name=requester]:checked')).map(x=>x.value);}));
  }
  function submitUpload(){
    let d=docs.find(x=>x.id===draft.docId);
    if(!d){d={id:'d'+(docs.length+1),title:draft.title,purpose:draft.purpose,owner:persona,acl:[...draft.acl],status:'Draft',published:null,filename:draft.file.name,versions:[],updated:'16 ต.ค. 2569'};docs.push(d);}
    else{d.title=draft.title;d.purpose=draft.purpose;d.acl=[...draft.acl];}
    d.versions.push({n:d.versions.length+1,name:draft.file.name,stage:0,state:'AwaitingValidation'});
    events.unshift({action:'Upload submitted · จำลอง',doc:d.title,actor:people[persona].name,result:'รอตรวจ · จำลอง'});draft=null;setPage('mine');manage(d);toast('เพิ่มสถานะส่งตรวจจำลองแล้ว ไม่มีไฟล์ถูกอัปโหลดจริง');
  }
  function renderReports(){
    $('#main').innerHTML=title('BUSINESS REPORTS','รายงาน','ระบบเลือก label ตามประเภทรายงานก่อนออกสำเนา')+`<section class="report-grid"><article class="panel report-card"><span class="file-icon">XLSX</span><h2>Headcount Summary</h2><p>สรุปจำนวนพนักงานด้วยข้อมูลสังเคราะห์ ไม่มีรายละเอียดเงินเดือนรายบุคคล</p><div>${badge('Internal · Label only')}</div><label class="field"><span class="field-label">รอบรายงาน</span><select aria-label="รอบรายงาน Headcount"><option>กันยายน 2569</option></select></label><button class="primary" data-action="report" data-id="headcount">จำลองขอรายงาน</button></article>${people[persona].payroll?`<article class="panel report-card"><span class="file-icon">XLSX</span><h2>Payroll Detail</h2><p>รายละเอียดเงินเดือนจำลอง สำหรับบทบาท PayrollExporter</p><div>${badge('Confidential · Admin-defined')}</div><label class="field"><span class="field-label">รอบรายงาน</span><select aria-label="รอบรายงาน Payroll"><option>กันยายน 2569</option></select></label><button class="primary" data-action="report" data-id="payroll">จำลองขอรายงาน</button></article>`:''}</section><div class="callout">Reports เลือก label ตามบริบทรายงาน · Document Library กำหนดผู้รับสิทธิ์เฉพาะแต่ละสำเนา ทั้งหมดในต้นแบบเป็นข้อมูลจำลอง</div>`;
  }
  function renderHistory(){
    const mine=jobs.filter(j=>j.user===persona);
    $('#main').innerHTML=title('MY REQUESTS','คำขอของฉัน','สถานะสำเนาที่ออกให้บัญชีตัวอย่างนี้เท่านั้น')+`<section class="panel">${mine.length?`<table class="doc-table"><thead><tr><th>คำขอ</th><th>ผู้รับสำเนา</th><th>สถานะจำลอง</th><th>รายละเอียด</th></tr></thead><tbody>${mine.map(j=>`<tr><td><strong>${esc(j.title)}</strong><span class="file-meta">${j.id}</span></td><td>${esc(people[j.user].name)} · ${j.user}</td><td>${badge(exportStages[j.stage],j.stage===5?'green':'amber')}</td><td><button class="secondary row-action" data-action="job" data-id="${j.id}">ดูสถานะ</button></td></tr>`).join('')}</tbody></table>`:'<div class="empty"><h2>ยังไม่มีคำขอของคุณ</h2><p>เริ่มจากเลือกเอกสาร แล้วขอสำเนาสำหรับตนเอง</p><button class="primary" data-page="library">ไปคลังเอกสาร</button></div>'}</section>`;
  }
  function renderAudit(){
    if(!people[persona].auditor)return setPage('library');
    $('#main').innerHTML=title('APPLICATION AUDIT','บันทึกการทำงาน','ตัวอย่าง application audit · ไม่มีสิทธิ์ดาวน์โหลดเอกสารจากบทบาท Auditor')+`<section class="panel"><table class="doc-table"><thead><tr><th>เหตุการณ์</th><th>เอกสาร</th><th>ผู้ทำรายการ</th><th>ผลลัพธ์</th></tr></thead><tbody>${events.map(e=>`<tr><td>${esc(e.action)}</td><td>${esc(e.doc)}</td><td>${esc(e.actor)}</td><td>${badge(e.result,'grey')}</td></tr>`).join('')}</tbody></table></section><div class="callout warning">รายการตัวอย่าง ไม่ใช่ Purview native audit หรือหลักฐานการสแกน/เข้ารหัสจริง ไม่มี token, ข้อมูลไฟล์ หรือปุ่มดาวน์โหลดข้ามผู้ใช้</div>`;
  }
  function renderLogin(){
    $('#main').innerHTML=`<section class="panel login-wrap"><div class="brand"><span class="brand-icon" aria-hidden="true"><b></b><b></b></span>folio.</div><p class="eyebrow">YOUR ORGANIZATION'S DOCUMENT WORKSPACE</p><h1>เอกสารพร้อมทำงาน<br>สิทธิ์พร้อมสำหรับคุณ</h1><p>เข้าสู่ระบบด้วยบัญชีองค์กร เพื่อเข้าถึงเอกสารและขอสำเนาที่มีสิทธิ์เฉพาะคุณ</p><button class="primary" data-action="demo-signin">ดูหน้าหลังเข้าสู่ระบบ (จำลอง Entra ID)</button><p class="subtle">ต้นแบบนี้ไม่เปิด Microsoft sign-in และไม่ขอรหัสผ่าน</p></section>`;
  }
  function confirmAction(heading,text,button,callback,danger=false){
    $('#confirm-content').innerHTML=`<div class="dialog-head"><h2 id="confirm-title">${esc(heading)}</h2><button class="dialog-close" data-close="confirm" aria-label="ปิดยืนยัน">×</button></div><div class="dialog-body"><p class="scope-note">${esc(text)}</p></div><div class="dialog-actions"><button class="secondary" data-close="confirm" autofocus>ยกเลิก</button><button class="${danger?'danger':'primary'}" id="confirm-action">${esc(button)}</button></div>`;
    $('#confirm-dialog').showModal();$('#confirm-action').addEventListener('click',()=>{$('#confirm-dialog').close();callback();},{once:true});
  }
  function editMetadata(d){
    $('#confirm-content').innerHTML=`<div class="dialog-head"><h2 id="confirm-title">รายละเอียดและผู้ขอสำเนา</h2><button class="dialog-close" data-close="confirm" aria-label="ปิด">×</button></div><div class="dialog-body"><div class="field"><label for="edit-title">ชื่อเอกสาร</label><input id="edit-title" type="text" maxlength="120" value="${esc(d.title)}"></div><div class="field"><label for="edit-purpose">วัตถุประสงค์</label><textarea id="edit-purpose">${esc(d.purpose)}</textarea></div>${['A','B'].map(id=>`<label class="check-option"><input type="checkbox" name="edit-acl" value="${id}" ${d.acl.includes(id)?'checked':''}><span>${esc(people[id].name)} · ${id} ขอตนเองได้</span></label>`).join('')}<p class="scope-note">Owner มาจากบัญชี · ไม่มีช่องเปลี่ยน Owner หรือกำหนดสิทธิ์ไฟล์ของผู้อื่น</p></div><div class="dialog-actions"><button class="secondary" data-close="confirm">ยกเลิก</button><button class="primary" id="save-meta">บันทึกตัวอย่าง</button></div>`;
    $('#confirm-dialog').showModal();$('#save-meta').addEventListener('click',()=>{const t=$('#edit-title').value.trim();if(!t)return toast('กรุณาระบุชื่อเอกสาร');d.title=t;d.purpose=$('#edit-purpose').value;d.acl=Array.from(document.querySelectorAll('[name=edit-acl]:checked')).map(x=>x.value);$('#confirm-dialog').close();manage(d);renderMine();toast('ปรับ metadata/ACL จำลองแล้ว');},{once:false});
  }
  document.addEventListener('click',e=>{
    const close=e.target.closest('[data-close]');if(close){$('#'+close.dataset.close+'-dialog').close();return;}
    const route=e.target.closest('[data-page]');if(route){setPage(route.dataset.page);return;}
    const el=e.target.closest('[data-action]');if(!el)return;
    const action=el.dataset.action,id=el.dataset.id,d=docs.find(x=>x.id===id),j=jobs.find(x=>x.id===id);
    if(action==='detail')detail(d);
    else if(action==='request')requestCopy(d);
    else if(action==='advance-job'){j.stage=Math.min(5,j.stage+1);showJob(j);}
    else if(action==='job')showJob(j);
    else if(action==='rights')rights(j);
    else if(action==='manage')manage(d);
    else if(action==='new')startWizard();
    else if(action==='new-version')startWizard(id);
    else if(action==='sample-file'){draft.file={name:'Project-plan.xlsx',size:132096};draft.error='';renderUpload();}
    else if(action==='wizard-next'){
      draft.error='';if(wizardStep===0&&(!draft.title.trim()||!draft.purpose.trim()||!draft.file))draft.error='กรุณาระบุชื่อ วัตถุประสงค์ และเลือกไฟล์ตัวอย่างก่อน';
      if(draft.error){renderUpload();$('#doc-title')?.focus();}else{wizardStep++;renderUpload();}
    }
    else if(action==='wizard-back'){wizardStep--;draft.error='';renderUpload();}
    else if(action==='cancel-upload')confirmAction('ยกเลิกฉบับร่างนี้?','ข้อมูลที่กรอกในต้นแบบยังไม่ได้ส่งตรวจ','ยกเลิกฉบับร่าง',()=>{draft=null;setPage('mine');});
    else if(action==='submit-upload')submitUpload();
    else if(action==='advance-intake'){const v=d.versions.find(v=>v.n===Number(el.dataset.version));v.stage=Math.min(3,v.stage+1);if(v.stage===3)v.state='Eligible';renderMine();manage(d);}
    else if(action==='reject-intake'){const v=d.versions.find(v=>v.n===Number(el.dataset.version));v.state='Rejected';renderMine();manage(d);}
    else if(action==='publish')confirmAction('เผยแพร่เวอร์ชันนี้?','ตัวอย่างการเปลี่ยนเวอร์ชันที่เผยแพร่ ผู้มีสิทธิ์จะขอสำเนาเวอร์ชันนี้ได้ คำขอของเวอร์ชันเก่าจะไม่ดาวน์โหลดผ่าน portal ได้','จำลองเผยแพร่',()=>{d.published=Number(el.dataset.version);d.status='Published';d.filename=d.versions.find(v=>v.n===d.published).name;events.unshift({action:'Publish · จำลอง',doc:d.title,actor:people[persona].name,result:'Published fixture'});renderMine();manage(d);toast('เผยแพร่เฉพาะ fixture ในต้นแบบแล้ว');});
    else if(action==='withdraw')confirmAction('ถอนเผยแพร่เอกสารนี้?','จะหยุดการขอและดาวน์โหลดผ่าน portal สำเนาที่ดาวน์โหลดไปแล้วไม่ได้ถูกถอนสิทธิ์โดยอัตโนมัติ','จำลองถอนเผยแพร่',()=>{d.status='Withdrawn';events.unshift({action:'Withdraw · จำลอง',doc:d.title,actor:people[persona].name,result:'Withdrawn fixture'});renderMine();manage(d);toast('ถอนเผยแพร่ fixture แล้ว');},true);
    else if(action==='edit-metadata')editMetadata(d);
    else if(action==='report'){const job={id:'EX-'+String(jobs.length+1).padStart(3,'0'),user:persona,title:id==='payroll'?'Payroll Detail':'Headcount Summary',stage:0};jobs.unshift(job);showJob(job);nav();}
    else if(action==='demo-signin')setPage(people[persona].publisher?'mine':people[persona].auditor?'audit':'library');
  });
  $('#persona').addEventListener('change',e=>{persona=e.target.value;document.querySelectorAll('dialog[open]').forEach(x=>x.close());draft=null;setPage(people[persona].publisher?'mine':people[persona].auditor?'audit':'library');});
  $('#menu-toggle').addEventListener('click',()=>{const opened=$('#sidebar').classList.toggle('open');$('#menu-toggle').setAttribute('aria-expanded',String(opened));});
  $('#sign-in-link').addEventListener('click',()=>setPage('login'));
  document.addEventListener('keydown',e=>{if(e.key==='Escape'){$('#sidebar').classList.remove('open');$('#menu-toggle').setAttribute('aria-expanded','false');}});
  $('.brand').addEventListener('click',e=>{e.preventDefault();setPage(people[persona].publisher?'mine':people[persona].auditor?'audit':'library');});
  render();
})();
