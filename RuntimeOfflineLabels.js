// Offline, display-only metadata. Never requests a service or writes game data.
(function(g){
'use strict';
var cfg=g.CelesphoniaControlConfig||{},fs,path,crypto;try{fs=require('fs');path=require('path');crypto=require('crypto');}catch(_){}
var file=fs&&cfg.home?path.join(cfg.home,'label-index.json'):null,bankFile=fs&&cfg.home?path.join(cfg.home,'label-translations.json'):null;
var index=null,bank={},translator=null,lastInput=null,error='';
try{if(bankFile&&fs.existsSync(bankFile))bank=JSON.parse(fs.readFileSync(bankFile,'utf8'));}catch(e){error='Label cache could not be read: '+e.message;}
function asian(s){return /[\u3040-\u30ff\u3400-\u9fff]/.test(s);}
function write(f,data){if(!f)return;var tmp=f+'.new';fs.writeFileSync(tmp,JSON.stringify(data,null,2),'utf8');fs.renameSync(tmp,f);}
function digest(s){return crypto?crypto.createHash('sha256').update(s,'utf8').digest('hex'):s;}
function ensure(local){translator=local;if(!g.$dataSystem||!g.$dataMapInfos)return;var input={variables:g.$dataSystem.variables,switches:g.$dataSystem.switches,maps:g.$dataMapInfos};if(index&&lastInput===g.$dataSystem)return;
 var raw=JSON.stringify(input),hash=digest(raw),dictionaryHash=digest(String(local.cacheKey||'')+JSON.stringify(bank)),prior=null;try{if(file&&fs.existsSync(file))prior=JSON.parse(fs.readFileSync(file,'utf8'));}catch(_){}
 if(prior&&prior.format===3&&prior.generation==='offline-labels-3.2'&&prior.sourceHash===hash&&prior.dictionaryHash===dictionaryHash&&prior.rows&&['variables','switches','maps'].every(function(k){return Array.isArray(prior.rows[k]);})){index=prior;lastInput=g.$dataSystem;return;}
 index={format:3,generation:'offline-labels-3.2',mode:'offline',sourceHash:hash,dictionaryHash:dictionaryHash,version:cfg.version,profile:cfg.profile,rows:{variables:[],switches:[],maps:[]}};lastInput=g.$dataSystem;
 Object.keys(input).forEach(function(kind){input[kind].forEach(function(v,id){if(!id||v==null)return;var source=String(kind==='maps'?v.name:v),cached=bank[source],old=prior&&prior.rows&&prior.rows[kind]&&prior.rows[kind][id],english=source?local(source):'(unnamed)',origin='bundled';
 if(cached&&cached.source===source&&typeof cached.english==='string'&&cached.english.trim()){english=cached.english;origin='local cache';}
 else if(old&&old.source===source&&typeof old.english==='string'&&!asian(old.english)&&asian(english)){english=old.english;origin='local cache';}
 index.rows[kind][id]={id:id,source:source,english:english,origin:origin,complete:!asian(english),parentId:kind==='maps'?v.parentId:undefined,order:kind==='maps'?v.order:undefined};});});
 try{write(file,index);}catch(e){error='Label index could not be saved: '+e.message;}
}
function name(kind,id,source){var r=index&&index.rows[kind]&&index.rows[kind][id];return r&&r.source===String(source||'')?r.english:null;}
function status(){var total=0,complete=0;if(index)Object.keys(index.rows).forEach(function(k){index.rows[k].forEach(function(r){if(r&&r.source){total++;if(r.complete)complete++;}});});return {total:total,complete:complete,unmatched:total-complete,mode:'offline',error:error};}
function importLabels(data){if(!data||typeof data!=='object'||Array.isArray(data))throw new Error('Import a JSON object mapping original names to English names.');var updates={};Object.keys(data).forEach(function(source){if(typeof data[source]!=='string'||!data[source].trim())throw new Error('Every imported label must be a nonempty string.');updates[source]={source:source,english:data[source].trim(),status:'imported'};});Object.keys(updates).forEach(function(k){bank[k]=updates[k];});write(bankFile,bank);index=null;ensure(translator);return Object.keys(updates).length;}
g.CelesphoniaLabelIndex={ensure:ensure,name:name,status:status,importLabels:importLabels,exportLabels:function(){return index;},file:file};
})(window);
