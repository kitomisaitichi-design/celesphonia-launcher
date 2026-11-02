(function(){
 'use strict';
 var fs=require('fs'),out=__OUTPUT__,fixed=__FIXED__,done=false,began=Date.now();
 var report={checks:[],cases:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 function plain(t){return String(t).replace(/(?:\\|\x1b)[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g,'');}
 function image(b,name){var p=out+'.'+name+'.png';fs.writeFileSync(p,new Buffer(b._canvas.toDataURL('image/png').split(',')[1],'base64'));return p;}
 window.addEventListener('error',function(e){finish(e.error||Error(e.message));});
 var timer=setInterval(function(){
  if(done)return;if(Date.now()-began>40000){finish(Error('Memory Library native startup timeout'));return;}
  if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title'||!ImageManager.loadSystem('IconSet').isReady())return;
  clearInterval(timer);
  try{
   CelesphoniaEnglish.captureSeen=false;check('English active without runtime controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls);
   DataManager.createGameObjects();$gameParty.setupStartingMembers();
   var nativeData=JSON.stringify([$dataWeapons,$dataItems,$dataArmors,$dataSkills,$dataStates,$dataCommonEvents,$dataTemplateEvents]),actor=$gameActors.actor(1),win=new Window_Base(0,0,1024,120),menu=new Game_Interpreter();
   var template=$dataTemplateEvents.filter(function(e){return e&&e.name==='EngramRelease';})[0];check('native library template found',!!template);
   var list=template.pages[0].list,pre=list.filter(function(c){return c.code===356&&/D_TEXT.*前提/.test(c.parameters[0]);})[0];check('native prerequisite D_TEXT command found',!!pre);
   report.variant=CelesphoniaEnglishMeta.version;report.template={id:template.id,name:template.name,command:pre.parameters[0]};report.records=[];report.references=[];
   $dataWeapons.forEach(function(w){if(!w)return;var m=w.meta||{},row={id:w.id,name:w.name,pre:Number(m.PreSkill||0),level:Number(m.PreLevel||0),repair:Number(m.Repair||0)};if(row.pre>0||row.level>0||row.repair>0)report.records.push(row);});
   function vars(id){$gameVariables._data[575]=161;$gameVariables._data[597]=id;$gameVariables._data[2747]=3;$gameVariables._data[2741]=17;$gameVariables._data[2742]=0;$gameVariables._data[2743]=6;$gameVariables._data[2744]=2;$gameVariables._data[3441]=23;$gameVariables._data[3442]=32;$gameVariables._data[577]=25;$gameVariables._data[229]=60;$gameVariables._data[598]='魂墨';$gameVariables._data[574]=20;$gameVariables._data[566]=0;}
   function row(command,label){
    $gameScreen.clearDTextPicture();var cmd={code:356,indent:0,parameters:[command]};menu.setup([cmd,{code:0,indent:0,parameters:[]}],0);menu.update();$gameScreen.showPicture(91,'',0,0,0,100,100,255,0);
    var before=JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameParty._weapons,$gameParty._items,$gameScreen._pictures]),calls=[],draw=Bitmap.prototype.drawText,s;
    Bitmap.prototype.drawText=function(t){calls.push(String(t));return draw.apply(this,arguments);};
    try{s=new Sprite_Picture(91);s.update();if(s.dTextInfo&&!s.bitmap)s.makeDynamicBitmap();}finally{Bitmap.prototype.drawText=draw;}
    check('native picture render preserves state '+label,before===JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameParty._weapons,$gameParty._items,$gameScreen._pictures]));
    return {label:label,source:command,value:s.dTextInfo&&s.dTextInfo.value,text:calls.join(''),bitmap:s.bitmap,width:s.bitmap&&s.bitmap.width,height:s.bitmap&&s.bitmap.height};
   }
   vars(160);var primary=row(pre.parameters[0],'reported');report.primary={source:primary.source,value:primary.value,text:primary.text,width:primary.width,height:primary.height};
   var panel=new Bitmap(Math.max(620,primary.width+32),primary.height+32);panel.fillAll('#10205f');panel.blt(primary.bitmap,0,0,primary.width,primary.height,16,16);report.primaryImage=image(panel,'prerequisite');report.glyphImage=image(primary.bitmap,'glyph');
   report.records.forEach(function(r){
    if(r.pre>0){vars(r.pre);var p=row(pre.parameters[0],'engram-'+r.id);report.cases.push({id:r.id,reference:r.pre,name:$dataWeapons[r.pre].name,expected:CelesphoniaEnglish.translate($dataWeapons[r.pre].name),text:p.text,value:p.value,width:p.width});}
   });
   var branches=list.filter(function(c){return c.code===356&&/D_TEXT/.test(c.parameters[0])&&/必要等级|必要修复度|消费：/.test(c.parameters[0]);});
   report.branches=[];branches.forEach(function(c,i){vars(160);var r=row(c.parameters[0],'branch-'+i);report.branches.push({source:c.parameters[0],text:r.text,value:r.value});});
   var groups=[['ii',$dataItems],['iw',$dataWeapons],['ia',$dataArmors],['is',$dataSkills],['it',$dataStates]];
   groups.forEach(function(pair){var code=pair[0],db=pair[1],count=0;db.forEach(function(item){if(!item||!item.name)return;var raw='\\'+code+'['+item.id+']',value=win.convertEscapeCharacters(raw);report.references.push({code:code,id:item.id,name:item.name,expected:CelesphoniaEnglish.translate(item.name),value:value,icon:item.iconIndex});count++;});report.checks.push({name:'icon-reference records enumerated '+code,pass:count>0,detail:count});});
   vars(160);var direct=win.convertEscapeCharacters('\\iw[160]'),variable=win.convertEscapeCharacters('\\iw[\\v[597]]');report.variableParity={direct:direct,variable:variable};
   $gameVariables._data[596]=597;report.nestedVariable=win.convertEscapeCharacters('\\iw[\\v[\\v[596]]]');
   var original=Window_Base.prototype.escapeIconItem;report.invalid=[];[0,999999].forEach(function(id){try{report.invalid.push({id:id,value:original.call(win,id,$dataWeapons)});}catch(e){report.invalid.push({id:id,error:e.name});}});
   report.eligibility=[];[0,1].forEach(function(owned){[22,32].forEach(function(level){[0,20].forEach(function(ink){vars(160);$gameParty._weapons[160]=owned;$gameVariables._data[3442]=level;$gameVariables._data[566]=ink;var state=JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameParty._weapons,$gameParty._items]);var met={owned:owned,level:level,ink:ink,prerequisite:!!(actor.isEquipedMagic(160)||$gameParty.hasItem($dataWeapons[160])),levelMet:$gameVariables.value(3442)>=$gameVariables.value(3441),costMet:$gameVariables.value(566)>=$gameVariables.value(574),repairMet:$gameVariables.value(229)>=$gameVariables.value(577)};win.convertEscapeCharacters(pre.parameters[0]);check('eligibility inputs preserved '+owned+'-'+level+'-'+ink,state===JSON.stringify([$gameVariables._data,$gameSwitches._data,$gameParty._weapons,$gameParty._items]));report.eligibility.push(met);});});});
   report.unknownIconNames=report.references.filter(function(r){return /[\u3040-\u30ff\u3400-\u9fff]/.test(r.expected);}).map(function(r){return{code:r.code,id:r.id,name:r.name};});
   check('all prerequisite metadata remains native',report.records.every(function(r){var m=$dataWeapons[r.id].meta;return Number(m.PreSkill||0)===r.pre&&Number(m.PreLevel||0)===r.level&&Number(m.Repair||0)===r.repair;}));
   check('all native databases and event/template code unchanged',nativeData===JSON.stringify([$dataWeapons,$dataItems,$dataArmors,$dataSkills,$dataStates,$dataCommonEvents,$dataTemplateEvents]));
   if(fixed){
    var before=JSON.parse(fs.readFileSync(out.replace(/after\.json$/,'before.json'),'utf8'));
    check('same complete prerequisite record set',JSON.stringify(report.records)===JSON.stringify(before.records));
    check('all prior-engram names translated',report.cases.every(function(c){return c.text.indexOf(c.expected)>=0&&!/[\u3040-\u30ff\u3400-\u9fff]/.test(plain(c.text));}),report.cases.filter(function(c){return c.text.indexOf(c.expected)<0;}));
    check('reported prerequisite English',report.primary.text.indexOf(CelesphoniaEnglish.translate($dataWeapons[160].name))>=0&&!/[\u3400-\u9fff]/.test(report.primary.text));
    check('all prerequisite lines fit native viewport',report.cases.every(function(c){return c.width<=Graphics.boxWidth;}));
    check('all known icon-name translations used',report.references.every(function(r){return r.value.indexOf(r.expected)>=0;}),report.references.filter(function(r){return r.value.indexOf(r.expected)<0;}).slice(0,10));
    check('icons preserved for all references',report.references.every(function(r){return r.value.toLowerCase().indexOf('\x1bi['+r.icon+']')>=0;}));
    check('direct and variable-derived IDs match',direct===variable&&direct===report.nestedVariable,report.variableParity);
    check('invalid IDs preserve native failure behavior',JSON.stringify(report.invalid)===JSON.stringify(before.invalid));
    check('eligibility matrix unchanged',JSON.stringify(report.eligibility)===JSON.stringify(before.eligibility));
    check('level, repair and currency branch numbers preserved',JSON.stringify(report.branches.map(function(r){return plain(r.text).match(/\d+(?:\.\d+)?/g);}))===JSON.stringify(before.branches.map(function(r){return plain(r.text).match(/\d+(?:\.\d+)?/g);}))); 
    check('available/unavailable source color escapes preserved',JSON.stringify(report.branches.map(function(r){return String(r.value).match(/\x1b[cC]\[\d+\]/g);}))===JSON.stringify(before.branches.map(function(r){return String(r.value).match(/\x1b[cC]\[\d+\]/g);})));
   }
   finish();
  }catch(e){finish(e);}
 },70);
})();
