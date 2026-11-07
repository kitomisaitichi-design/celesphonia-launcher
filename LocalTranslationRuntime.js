/* Celesphonia local translation: display copies only; never waits for a model. */
(function(g){
 'use strict';var cfg=g.CelesphoniaLocalConfig;if(!cfg||!g.require)return;
 var decoder=new (require('string_decoder').StringDecoder)('utf8');
 var fs=require('fs'),path=require('path'),binding=null,run=null,exact=Object.create(null),unique=Object.create(null),ambiguous=Object.create(null),targets=Object.create(null),known=Object.create(null),seen=Object.create(null),pending=[],position=0,carry='',loading=false,activeInterpreter=null,sourceLists=typeof WeakMap==='function'?new WeakMap():null;
 function read(file){try{return JSON.parse(fs.readFileSync(file,'utf8').replace(/^\ufeff/,''));}catch(e){if(e.code==='ENOENT')return null;g.CelesphoniaLocalError=String(e.message||e);return null;}}
 binding=read(cfg.binding);g.CelesphoniaLocalMode=!!(binding&&binding.Enabled);
 if(!g.CelesphoniaLocalMode)return;
 function validId(id){return typeof id==='string'&&/^[a-f0-9]{32}$/.test(id);}
 function runPath(id,file){return path.join(cfg.home,'Runs',id,file);}
 var cacheRevision=0;
 function installRun(id){cacheRevision++;exact=Object.create(null);unique=Object.create(null);ambiguous=Object.create(null);targets=Object.create(null);position=0;carry='';decoder=new (require('string_decoder').StringDecoder)('utf8');run=validId(id)?read(runPath(id,'run.json')):null;if(run&&run.GameKey!==cfg.gameKey)run=null;}
 installRun(binding.RunId);
 function accept(row){
  if(!row||typeof row.Source!=='string'||typeof row.Target!=='string'||typeof row.Context!=='string')return;
  exact[row.Context+'\u001f'+row.Category+'\u001f'+row.Source]=row.Target;targets[row.Target]=true;cacheRevision++;
  var key=row.Category+'\u001f'+row.Source;if(Object.prototype.hasOwnProperty.call(unique,key)&&unique[key]!==row.Target){ambiguous[key]=true;delete unique[key];}else if(!ambiguous[key])unique[key]=row.Target;
 }
 function tail(){
  if(!run||loading)return;var file=runPath(run.Id,'results.jsonl'),size;
  try{size=fs.statSync(file).size;}catch(e){if(e.code!=='ENOENT')g.CelesphoniaLocalError=String(e);return;}
  if(size<position){installRun(run.Id);return;}if(size===position)return;
  loading=true;var id=run.Id,at=position;
  fs.open(file,'r',function(error,fd){if(error){loading=false;return;}var buffer=new Buffer(Math.min(65536,size-at));fs.read(fd,buffer,0,buffer.length,at,function(err,n){fs.close(fd,function(){});loading=false;if(err||!run||run.Id!==id)return;position+=n;carry+=decoder.write(buffer.slice(0,n));var lines=carry.split('\n');carry=lines.pop();lines.forEach(function(line){if(!line.trim())return;try{accept(JSON.parse(line));}catch(e){g.CelesphoniaLocalError='Incomplete cache entry; waiting for next checkpoint';}});if(position<size)setTimeout(tail,10);});});
 }
 function category(text,fallback){var c=known[text];return c||fallback||'Interface';}
 function context(interpreter){
  if(interpreter&&sourceLists){var found=sourceLists.get(interpreter._list);if(found)return found+'/'+interpreter._index;}
  if(interpreter&&g.$gameMap){var ev=$gameMap.event(interpreter._eventId),id=$gameMap.mapId();if(ev&&ev.event()&&ev.event().pages)return 'Map'+('000'+id).slice(-3)+'/event'+interpreter._eventId+'/pages/'+ev._pageIndex+'/list/'+interpreter._index;}
  return 'Live';
 }
 function queue(text,kind,ctx){
  if(!run||!binding||!binding.Enabled||run.Scope.indexOf(kind)<0||!text.trim()||text.length>12000||!/[A-Za-z\u00c0-\uffff]/.test(text)||/(?:\$game|prototype\.|https?:\/\/|\.(?:png|ogg|rpgmvp|json|js)$)/i.test(text))return;
  var key=ctx+'\u001f'+kind+'\u001f'+text;if(seen[key])return;seen[key]=true;pending.push({Source:text,Category:kind,Context:ctx});
 }
 function resolve(text,kind,ctx){
  if(typeof text!=='string'||!run||!binding||!binding.Enabled||targets[text])return text;registerDatabase();
  kind=category(text,kind);if(run.Scope.indexOf(kind)<0)return text;ctx=ctx||context(activeInterpreter);
  var converted=text.indexOf('\x1b')>=0,raw=converted?text.replace(/\x1b/g,'\\'):text,key=ctx+'\u001f'+kind+'\u001f'+raw,result=exact[key];
  if(result===undefined)result=unique[kind+'\u001f'+raw];
  if(result===undefined){var lines=raw.split('\n');if(lines.length>1){var changed=false;result=lines.map(function(line){var value=unique[kind+'\u001f'+line];if(value!==undefined){changed=true;return value;}queue(line,kind,ctx);return line;}).join('\n');if(!changed)result=undefined;}}
  if(result===undefined){queue(raw,kind,ctx);return text;}return converted?result.replace(/\\/g,'\x1b'):result;
 }
 function wrap(proto,name,fn){if(!proto||typeof proto[name]!=='function'||proto[name]._localTL)return;var old=proto[name],next=fn(old);next._localTL=true;proto[name]=next;}
 var databaseReady=false;
 function registerDatabase(){
  if(g.$dataMap&&$dataMap.displayName)known[$dataMap.displayName]='Names';if(databaseReady||!g.$dataItems||!g.$dataSkills||!g.$dataCommonEvents)return;databaseReady=true;
  ['$dataActors','$dataClasses','$dataItems','$dataWeapons','$dataArmors','$dataEnemies','$dataSkills','$dataStates','$dataMapInfos'].forEach(function(name){var rows=g[name];if(!rows)return;rows.forEach(function(row){if(!row)return;['name','nickname','displayName'].forEach(function(k){if(row[k])known[row[k]]='Names';});if(row.description){known[row.description]='Descriptions';row.description.split('\n').forEach(function(line){if(line)known[line]='Descriptions';});}});});
  if(sourceLists)$dataCommonEvents.forEach(function(ev){if(ev&&ev.list)sourceLists.set(ev.list,'CommonEvents/'+ev.id+'/list');});
 }
 function paintText(old){return function(text){var args=Array.prototype.slice.call(arguments);args[0]=resolve(text,'Interface');return old.apply(this,args);};}
 if(g.Bitmap)wrap(Bitmap.prototype,'drawText',function(old){return function(text,x,y,width,height,align){var prior=this.fontFace;this.fontFace=prior+', Segoe UI, Arial, sans-serif';try{return paintText(old).apply(this,arguments);}finally{this.fontFace=prior;}};});
 if(g.Window_Base){
  wrap(Window_Base.prototype,'convertEscapeCharacters',function(old){return function(text){return old.call(this,resolve(text,'Interface'));};});
  wrap(Window_Base.prototype,'drawTextEx',paintText);
  wrap(Window_Base.prototype,'actorName',function(old){return function(id){return resolve(old.apply(this,arguments),'Names','Actors/'+id);};});
  wrap(Window_Base.prototype,'partyMemberName',function(old){return function(index){return resolve(old.apply(this,arguments),'Names');};});
  wrap(Window_Base.prototype,'escapeIconItem',function(old){return function(id,database){var result=old.apply(this,arguments),item=database[id];if(item&&typeof result==='string'&&item.name&&result.slice(-item.name.length)===item.name){var names=[g.$dataItems,g.$dataWeapons,g.$dataArmors,g.$dataSkills,g.$dataStates],labels=['Items','Weapons','Armors','Skills','States'],at=names.indexOf(database);return result.slice(0,-item.name.length)+resolve(item.name,'Names',at>=0?labels[at]+'/'+id:'Live');}return result;};});
 }
 if(g.Game_Message)wrap(Game_Message.prototype,'add',function(old){return function(text){this._localTLContext=context(activeInterpreter);return old.call(this,resolve(text,'Dialogue',this._localTLContext));};});
 if(g.Game_Interpreter){
  ['command101','command105'].forEach(function(name){wrap(Game_Interpreter.prototype,name,function(old){return function(){var prior=activeInterpreter;activeInterpreter=this;try{return old.apply(this,arguments);}finally{activeInterpreter=prior;}};});});
  wrap(Game_Interpreter.prototype,'setupChoices',function(old){return function(){if(g.$gameMessage)$gameMessage._localTLChoiceContext=context(this);return old.apply(this,arguments);};});
 }
 if(g.Window_Command)wrap(Window_Command.prototype,'addCommand',function(old){return function(name,symbol,enabled,ext){var isChoice=g.Window_ChoiceList&&this instanceof Window_ChoiceList;return old.call(this,resolve(name,isChoice?'Choices':'Interface',isChoice&&g.$gameMessage?$gameMessage._localTLChoiceContext:null),symbol,enabled,ext);};});
 // Reuse the Celesphonia help-panel strategy: wrap at readable size, then
 // scroll dense text. Keep all effects and control codes; never shorten data.
 function helpWidth(win,text){var s=win.convertEscapeCharacters(text),icons=s.match(/\x1bi\[\d+\]/gi)||[];return win.textWidth(s.replace(/\x1b[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g,'').replace(/\x1b[.$|!><^{}\\]/g,''))+icons.length*(Window_Base._iconWidth+4);}
 function helpLayout(win,text){
  var original=win.contents.fontSize,width=Math.max(1,win.contentsWidth()-win.textPadding()-8),height=win.contentsHeight(),layout;
  for(var size=win.standardFontSize();size>=20;size-=2){
   win.contents.fontSize=size;var lines=[],widest=0;
   text.split('\n').forEach(function(paragraph){
    var line='';
    paragraph.replace(/\]\[/g,'] [').split(/(\s+)/).forEach(function(word){
     if(!word)return;var next=line+word;
     if(line&&helpWidth(win,next)>width){lines.push(line.replace(/\s+$/,''));line='';word=word.replace(/^\s+/,'');}
     if(helpWidth(win,word)>width){
      var tokens=word.match(/(?:\\|\x1b)(?:[A-Za-z]+(?:\[[^\]\r\n]*\]|<[^>\r\n]*>)?|[.$|!><^{}\\])|[\uD800-\uDBFF][\uDC00-\uDFFF]|[\s\S]/g)||[];
      tokens.forEach(function(token){if(line&&helpWidth(win,line+token)>width){lines.push(line);line='';}line+=token;});
     }else line+=word;
    });lines.push(line.replace(/\s+$/,''));
   });
   lines.forEach(function(line){widest=Math.max(widest,helpWidth(win,line));});
   var pitch=Math.max(size+8,/(?:\\|\x1b)i\[/i.test(text)?Window_Base._iconHeight:0);
   layout={text:lines.join('\n'),fontSize:size,pitch:pitch,height:lines.length*pitch,width:widest,availableWidth:width,viewportHeight:height,scrollable:lines.length*pitch>height};
   if(layout.height<=height)break;
  }win.contents.fontSize=original;return layout;
 }
 function helpHover(win){return win&&win.visible&&win.openness===255&&win._localHelpLayout&&win._localHelpLayout.scrollable&&TouchInput.x>=win.x&&TouchInput.x<win.x+win.width&&TouchInput.y>=win.y&&TouchInput.y<win.y+win.height;}
 if(g.Window_Help){
  wrap(Window_Help.prototype,'setText',function(old){return function(text){this._localHelpSource=text;return old.call(this,resolve(text,'Descriptions'));};});
  wrap(Window_Help.prototype,'refresh',function(old){return function(){
   var source=this._localHelpSource==null?this._text:this._localHelpSource,display=resolve(source,'Descriptions');this._localHelpRevision=cacheRevision;this._text=display;
   this.origin.y=0;this._localHelpLayout=null;this.upArrowVisible=this.downArrowVisible=false;
   if(!display||display===source){if(this.contents&&this.contents.height!==this.contentsHeight())this.createContents();return old.apply(this,arguments);}
   var layout=this._localHelpLayout=helpLayout(this,display),h=Math.max(this.contentsHeight(),layout.height);
   if(this.contents.height!==h)this.contents=new Bitmap(this.contentsWidth(),h);this.contents.clear();
   var reset=this.resetFontSettings,calc=this.calcTextHeight,hadReset=Object.prototype.hasOwnProperty.call(this,'resetFontSettings'),hadCalc=Object.prototype.hasOwnProperty.call(this,'calcTextHeight'),prior=this.contents.fontSize;
   this.resetFontSettings=function(){reset.call(this);this.contents.fontSize=layout.fontSize;};this.calcTextHeight=function(){return layout.pitch;};
   try{this.drawTextEx(layout.text,this.textPadding(),0);}finally{if(hadReset)this.resetFontSettings=reset;else delete this.resetFontSettings;if(hadCalc)this.calcTextHeight=calc;else delete this.calcTextHeight;this.contents.fontSize=prior;}
   this.downArrowVisible=layout.scrollable;
  };});
  wrap(Window_Help.prototype,'update',function(old){return function(){var result=old.apply(this,arguments);if(this._localHelpRevision!==cacheRevision&&this._localHelpSource!=null)this.refresh();var layout=this._localHelpLayout;if(layout&&layout.scrollable){var limit=Math.max(0,this.contents.height-this.contentsHeight());if(helpHover(this)&&Math.abs(TouchInput.wheelY)>=20)this.origin.y=Math.max(0,Math.min(limit,this.origin.y+Math.sign(TouchInput.wheelY)*layout.pitch*2));this.upArrowVisible=this.origin.y>0;this.downArrowVisible=this.origin.y<limit;}return result;};});
  if(g.Window_Selectable)wrap(Window_Selectable.prototype,'processWheel',function(old){return function(){if(g.SceneManager&&SceneManager._scene&&helpHover(SceneManager._scene._helpWindow))return;return old.apply(this,arguments);};});
 }
 if(g.Window_MagicExplain)wrap(Window_MagicExplain.prototype,'drawDescription',function(old){return function(text){var args=Array.prototype.slice.call(arguments);args[0]=resolve(text,'Descriptions');return old.apply(this,args);};});
 // Celesphonia's detail panels color fields before their final bitmap draw.
 if(g.Window_ObjectInfo)wrap(Window_ObjectInfo.prototype,'addBasicColors',function(old){return function(text){var args=Array.prototype.slice.call(arguments);args[0]=resolve(text,'Interface');return old.apply(this,args);};});
 function flush(){
  if(!pending.length)return;var rows=pending.splice(0,80),folder=path.join(cfg.home,'Incoming');try{if(!fs.existsSync(folder))fs.mkdirSync(folder);}catch(e){g.CelesphoniaLocalError=String(e);return;}
  fs.appendFile(path.join(folder,cfg.gameKey+'.jsonl'),rows.map(function(row){return JSON.stringify(row);}).join('\n')+'\n','utf8',function(e){if(e)g.CelesphoniaLocalError=String(e);});
 }
  var ticks=0;setInterval(function(){registerDatabase();tail();if(++ticks%5===0){var next=read(cfg.binding);if(next){var changed=!binding||next.RunId!==binding.RunId||next.Enabled!==binding.Enabled;binding=next;if(changed)installRun(binding.RunId);}flush();}},200);
 g.CelesphoniaLocalTranslation={translate:resolve,flush:flush,status:function(){return {enabled:!!(binding&&binding.Enabled),run:run&&run.Id,language:run&&run.Language,entries:Object.keys(exact).length,error:g.CelesphoniaLocalError||null};},reload:function(){var next=read(cfg.binding);if(next){binding=next;installRun(binding.RunId);tail();}}};tail();
})(window);
