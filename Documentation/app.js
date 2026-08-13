const search=document.querySelector('#search');
const sections=[...document.querySelectorAll('main>section')];
search.addEventListener('input',()=>{const q=search.value.trim().toLowerCase();sections.forEach(s=>s.hidden=q&&!(`${s.dataset.search||''} ${s.textContent}`).toLowerCase().includes(q))});
const links=[...document.querySelectorAll('nav a')];
const observer=new IntersectionObserver(entries=>entries.forEach(e=>{if(!e.isIntersecting)return;links.forEach(a=>a.classList.toggle('active',a.hash===`#${e.target.id}`))}),{rootMargin:'-20% 0px -70%'});
document.querySelectorAll('main>[id]').forEach(x=>observer.observe(x));

const api=window.GAME_SYSTEMS_API;
const apiIndex=document.querySelector('#apiIndex');
const apiContent=document.querySelector('#apiContent');
const apiSearch=document.querySelector('#apiSearch');
const includeEditor=document.querySelector('#includeEditor');
const apiStats=document.querySelector('#apiStats');
const documentationStamp=document.querySelector('#documentationStamp');
const versionBadge=document.querySelector('#versionBadge');
let selectedType=null;

const escapeHtml=value=>String(value||'').replace(/[&<>"']/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
const typeId=type=>`api-${type.fullName.replace(/[^A-Za-z0-9_-]/g,'-')}`;

function renderModuleNavigation(){
  const container=document.querySelector('#moduleNavigation');
  const categories=['Data','Runtime','Actions','Conditions','Serializable Data','Interfaces','Utilities'];
  const categoryIcons={Data:'feedback-asset',Runtime:'motor',Actions:'effect',Conditions:'condition','Serializable Data':'core',Interfaces:'hooks',Utilities:'core'};
  const modules={};
  api.types.filter(type=>!type.editor).forEach(type=>(modules[type.apiModule]??=[]).push(type));
  container.innerHTML=Object.entries(modules).map(([module,types])=>{
    const groups=categories.map(category=>[category,types.filter(type=>type.category===category)]).filter(([,items])=>items.length);
    return `<details class="module-category"><summary><img src="icons/${escapeHtml(types[0].moduleIcon)}.png" alt=""><strong>${escapeHtml(module)}</strong><span>${types.length}</span></summary><div>${groups.map(([category,items])=>`<details class="type-category"><summary><img src="icons/${escapeHtml(categoryIcons[category])}.png" alt=""><strong>${escapeHtml(category)}</strong><span>${items.length}</span></summary><div>${items.map(type=>`<a href="#${typeId(type)}" data-api-type="${escapeHtml(type.fullName)}"><img src="icons/${escapeHtml(type.icon)}.png" alt="">${escapeHtml(type.name)}</a>`).join('')}</div></details>`).join('')}</div></details>`;
  }).join('');
  container.querySelectorAll('[data-api-type]').forEach(link=>link.addEventListener('click',event=>{
    event.preventDefault();
    showType(link.dataset.apiType);
    document.querySelector('#api').scrollIntoView({behavior:'smooth'});
  }));
}

function visibleTypes(){
  if(!api)return[];
  const query=apiSearch.value.trim().toLowerCase();
  return api.types.filter(type=>(includeEditor.checked||!type.editor)&&(!query||`${type.fullName} ${type.kind} ${type.declaration} ${type.members.map(member=>`${member.name} ${member.signature}`).join(' ')}`.toLowerCase().includes(query)));
}

function renderApiIndex(){
  const groups={};
  visibleTypes().forEach(type=>(groups[type.apiModule]??=[]).push(type));
  apiIndex.innerHTML=Object.entries(groups).map(([module,types])=>`<details open><summary>${escapeHtml(module)} <span>${types.length}</span></summary><div>${types.map(type=>`<button type="button" class="api-type-link${selectedType===type.fullName?' selected':''}" data-type="${escapeHtml(type.fullName)}"><img src="icons/${escapeHtml(type.icon)}.png" alt=""><small>${escapeHtml(type.category)}</small>${escapeHtml(type.name)}</button>`).join('')}</div></details>`).join('')||'<p class="api-empty">Aucun type trouvé.</p>';
  apiIndex.querySelectorAll('[data-type]').forEach(button=>button.addEventListener('click',()=>showType(button.dataset.type)));
}

function showType(fullName){
  const type=api.types.find(candidate=>candidate.fullName===fullName);
  if(!type)return;
  selectedType=fullName;
  renderApiIndex();
  const groups={constructor:[],property:[],method:[],event:[],field:[],'enum value':[]};
  type.members.forEach(member=>(groups[member.kind]??=[]).push(member));
  const labels={constructor:'Constructeurs',property:'Propriétés',method:'Méthodes',event:'Événements',field:'Champs','enum value':'Valeurs'};
  const members=Object.entries(groups).filter(([,items])=>items.length).map(([kind,items])=>`<section class="member-group"><h3>${labels[kind]} <span>${items.length}</span></h3>${items.map(member=>`<article class="api-member" id="${typeId(type)}-${escapeHtml(member.name)}"><div class="member-meta"><span>${escapeHtml(member.visibility)}</span><a href="#${typeId(type)}-${escapeHtml(member.name)}">${escapeHtml(member.name)}</a></div><pre>${escapeHtml(member.signature)}</pre>${member.summary?`<p>${escapeHtml(member.summary)}</p>`:''}<small>${escapeHtml(type.file)}:${member.line}</small></article>`).join('')}</section>`).join('');
  apiContent.innerHTML=`<header class="type-header" id="${typeId(type)}"><div class="type-badges"><span>${escapeHtml(type.apiModule)}</span><span>${escapeHtml(type.category)}</span><span>${escapeHtml(type.kind)}</span>${type.editor?'<span>Editor</span>':''}</div><h2><img src="icons/${escapeHtml(type.icon)}.png" alt="">${escapeHtml(type.name)}</h2><p class="namespace">${escapeHtml(type.namespace)}</p><pre>${escapeHtml(type.declaration)}</pre>${type.summary?`<p>${escapeHtml(type.summary)}</p>`:''}<p class="source-path">${escapeHtml(type.file)}:${type.line}</p></header>${members||'<p class="api-empty">Aucun membre exposé déclaré dans ce type.</p>'}`;
  apiContent.scrollTop=0;
}

function refreshApi(){
  renderApiIndex();
  const types=visibleTypes();
  if(!types.some(type=>type.fullName===selectedType)&&types.length)showType(types[0].fullName);
  if(!types.length)apiContent.innerHTML='<p class="api-empty">Aucun résultat.</p>';
}

if(api){
  const release=`v${api.version} · ${api.generatedAt}`;
  documentationStamp.textContent=release;
  versionBadge.textContent=release;
  apiStats.textContent=`v${api.version} · ${api.typeCount} types · ${api.memberCount} membres · ${api.generatedAt}`;
  apiSearch.addEventListener('input',refreshApi);
  includeEditor.addEventListener('change',refreshApi);
  renderModuleNavigation();
  refreshApi();
}else{
  apiContent.innerHTML='<p class="api-empty">api-data.js est absent. Lancez generate-api.rb.</p>';
}
