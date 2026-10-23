(function(){
 'use strict';
 var fs=require('fs'),crypto=require('crypto'),out=__OUTPUT__,fixed=__FIXED__,done=false,start=Date.now();
 var report={checks:[],cases:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function hash(s){return crypto.createHash('sha256').update(s).digest('hex').slice(0,12);}
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name);}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 function capture(text){var glyphs=[],draw=Bitmap.prototype.drawText;Bitmap.prototype.drawText=function(s){glyphs.push(String(s));return draw.apply(this,arguments);};try{TickerManager.show(text,false);return glyphs.join('');}finally{Bitmap.prototype.drawText=draw;}}
 function image(suffix){var scene=SceneManager._scene,c=scene._tickerLogContainer;if(!c||!c.children.length)c=scene._tickerContainer;var t=c&&c.children[c.children.length-1];if(!t||!t.contents||!t.contents.contents)throw Error('Missing native ticker');var source=t.contents.contents,b=new Bitmap(1024,80);b.fillAll('#14101c');b.blt(source,0,0,Math.min(source.width,1024),Math.min(source.height,80),0,0);fs.writeFileSync(out+'.'+suffix+'.png',new Buffer(b._canvas.toDataURL('image/png').split(',')[1],'base64'));}
 var timer=setInterval(function(){
  if(Date.now()-start>35000){clearInterval(timer);finish(Error('Native startup timeout'));return;}
  if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
  clearInterval(timer);
  try{
   DataManager.createGameObjects();$gameParty.setupStartingMembers();
   check('normal English without controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls,{});
   var canonical=JSON.stringify([$dataActors,$dataSkills,$dataCommonEvents]),actor=$gameActors.actor(1),dict=CelesphoniaEnglishDictionary,keys=Object.keys(dict);
   var wanted=['c2c8df196340','2aa29223c4f7','58ec48b3071c'],real=keys.filter(function(k){return wanted.indexOf(hash(k))>=0;});
   check('all reported dictionary records found',real.length===3,{ids:real.map(hash)});
   var normalize=function(s){return s.replace(/\x1b/g,'\\').replace(/\\+/g,'\\');},callers=[];
   $dataCommonEvents.forEach(function(e){if(!e)return;e.list.forEach(function(c,index){if(c.code!==356||!/^TMng /.test(c.parameters[0]))return;real.forEach(function(k){if(normalize(c.parameters[0]).indexOf(normalize(k))>=0&&!callers.some(function(x){return x.key===k;}))callers.push({key:k,event:e.id,index:index,args:c.parameters[0].slice(5).split(' ')});});});});
   check('reported native callers found',callers.length===3,{});
   ['天音','苍蓝星','Mira'].forEach(function(name){actor.setName(name);callers.forEach(function(c){var ip=new Game_Interpreter();ip.pluginCommand('TMng',c.args);var id={Noicon:2950,Heart:2980,Talk:2944}[c.args[0]];check('native mode mapped '+c.args[0],!!id,{});$gameVariables._data[2995]=id;$gameVariables._data[118]=27;$gameVariables._data[119]=892;var snapshot=JSON.stringify([$gameVariables._data,$gameActors._data]),read=$gameVariables.value,text=capture('\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]');var row={sourceId:hash(c.key),event:c.event,index:c.index,mode:c.args[0],messageVariable:id,actor:CelesphoniaEnglish.translate(name),namePresent:text.indexOf(CelesphoniaEnglish.translate(name))>=0,visibleCharacters:text.length};report.cases.push(row);check('native values and reader restored '+report.cases.length,snapshot===JSON.stringify([$gameVariables._data,$gameActors._data])&&read===$gameVariables.value,{});});});
   actor.setName('Mira');var neutral='\\\\n[1]完成了检查。';dict[neutral]='\\\\n[1] completed the check.';
   $gameVariables._data[2950]=neutral.replace(/\\/g,'\x1b');$gameVariables._data[2995]=2950;
   var neutralText=capture('\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]');image('name');report.neutral={namePresent:neutralText.indexOf('Mira')>=0};
   var only='\\\\n[1]';dict[only]=only;$gameVariables._data[2950]=only.replace(/\\/g,'\x1b');var onlyText=capture('\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]');image('blank');report.nameOnly={namePresent:onlyText.indexOf('Mira')>=0,visibleCharacters:onlyText.length};
   var literal='未翻译的字段',original=$gameVariables.value;$gameVariables._data[2950]=literal;capture('\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]');check('unknown field remains native',$gameVariables._data[2950]===literal&&$gameVariables.value===original,{});
   report.skill955={name:CelesphoniaEnglish.translate($dataSkills[955].name),description:CelesphoniaEnglish.translate($dataSkills[955].description),effects:[95,110].map(function(id){return{id:id,tag:$tag_skill_list[id]};})};
   var card=new Window_Base(0,0,1024,180);card.contents.fontSize=24;card.contents.fillAll('#14101c');card.drawTextEx($dataSkills[955].name,0,0);card.drawTextEx(report.skill955.description.split('\n').slice(1).join('\n'),0,56);fs.writeFileSync(out+'.title.png',new Buffer(card.contents._canvas.toDataURL('image/png').split(',')[1],'base64'));
   var modes=[2944,2947,2950,2953,2957,2961,2964,2966,2968,2970,2972,2976,2980,2981,2983,2984,2986,2988,2989,2990,2991,2993,2994];report.modeCoverage=[];
   modes.forEach(function(id){$gameVariables._data[id]=neutral.replace(/\\/g,'\x1b');$gameVariables._data[2995]=id;var text=capture('\\i[\\v[119]]\\c[\\v[118]]Comment : \\v[\\v[2995]]');report.modeCoverage.push({id:id,namePresent:text.indexOf('Mira')>=0,prefixPresent:text.indexOf('Comment : ')>=0});});
   var multi='\\\\n[1]完成了检查。\n\n记录已保留。';dict[multi]='\\\\n[1] completed the check.\n\nRecord retained.';$gameVariables._data[2950]=multi.replace(/\\/g,'\x1b');$gameVariables._data[2995]=2950;
   var multiRaw=$gameVariables._data[2950];capture('\\i[\\v[119]]\\c[\\v[118]]\\v[\\v[2995]]');check('intentional native newlines remain stored',$gameVariables._data[2950]===multiRaw,{});
   check('native database unchanged',canonical===JSON.stringify([$dataActors,$dataSkills,$dataCommonEvents]),{});
   if(fixed){check('reported notifications retain names',report.cases.every(function(c){return c.namePresent;}),{});check('neutral nested field retains name',report.neutral.namePresent,{});check('name-only field is no longer blank',report.nameOnly.namePresent,{});check('all native modes and alternate frames retain names',report.modeCoverage.every(function(c){return c.namePresent&&c.prefixPresent;}),{});check('anal terminology follows native effects',!/Intestin/i.test(report.skill955.name+' '+report.skill955.description)&&/Anal|Anus/i.test(report.skill955.name+' '+report.skill955.description),{});}
   finish();
  }catch(e){finish(e);}
 },100);
})();
