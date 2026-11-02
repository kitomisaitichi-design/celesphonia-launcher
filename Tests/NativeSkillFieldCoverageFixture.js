(function(){
 'use strict';
 var fs=require('fs'),out=__OUTPUT__,fixed=__FIXED__,captureOnly=__CAPTURE_ONLY__,done=false,began=Date.now(),phase='boot',ticks=0;
 var report={checks:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false},canonical,scene,enemy,info,states,object,anchor;
 if(window.CelesphoniaEnglish)CelesphoniaEnglish.captureSeen=false;
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);if(e&&window.SceneManager){var s=SceneManager._scene;report.startup={scene:s&&s.constructor.name,ready:s&&s.isReady(),started:SceneManager._sceneStarted,active:s&&s._active,stopped:SceneManager._stopped,windows:s&&Object.keys(s).filter(function(k){return /[Ww]indow/.test(k);}),error:Graphics._errorPrinter&&Graphics._errorPrinter.textContent};}fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 function capture(suffix,rect){var visible=scene.children.map(function(c){return[c,c.visible];});scene.children.forEach(function(c){if(c!==scene._windowLayer&&c!==scene._objInfoWindow)c.visible=false;});var b;try{b=Bitmap.snap(scene);}finally{visible.forEach(function(p){p[0].visible=p[1];});}if(rect){var part=new Bitmap(Math.ceil(rect.width),Math.ceil(rect.height));part.blt(b,rect.x,rect.y,rect.width,rect.height,0,0);b=part;}var path=out+'.'+suffix+'.png';fs.writeFileSync(path,new Buffer(b._canvas.toDataURL('image/png').split(',')[1],'base64'));return path;}
 function plain(text){return String(text).replace(/(?:\\|\x1b)[A-Za-z]+(?:\[[^\]]*\]|<[^>]*>)?/g,'');}
 function draws(win,action){var calls=[],draw=Bitmap.prototype.drawText;Bitmap.prototype.drawText=function(text,x,y,width,height,align){if(this===win.contents)calls.push({text:String(text),x:x,y:y,width:this.measureTextWidth(String(text)),limit:width,height:height,font:this.fontSize,align:align});return draw.apply(this,arguments);};try{action();}finally{Bitmap.prototype.drawText=draw;}return calls;}
 function objectReport(){return{width:object.width,height:object.height,x:object.x,y:object.y,columns:object.maxCols(),font:object.standardFontSize(),contentsHeight:object.contents.height,viewportHeight:object.height-object.standardPadding()*2,settings:object._windowSet,rows:object._data.slice(),layout:object._celesphoniaEnglishStateInfo||null};}
 window.addEventListener('error',function(e){finish(e.error||Error(e.message));});
 var nativeCatch=SceneManager.catchException;SceneManager.catchException=function(e){nativeCatch.call(this,e);finish(e);};
 setInterval(function(){if(!done&&phase!=='boot')SceneManager.update();},16);
 var timer=setInterval(function(){
  if(done)return;if(Date.now()-began>42000){finish(Error('Native enemy-info timeout in '+phase));return;}
  try{
   if(phase==='boot'){
    if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
    check('English active without runtime controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls);
    DataManager.createGameObjects();$gameParty.setupStartingMembers();
    // Start from native battle prerequisites, not the pre-prologue civilian
    // with no MP. Difficulty 4 is the reported enemy's 2.5x HP / 2x ATK tier.
    $gameVariables.setValue(4980,4);$gameVariables.setValue(1259,100);$gameVariables.setValue(1030,18);
    var gaugeParameters=PluginManager.parameters('FTKR_CustomSimpleActorStatus'),gaugeInputs={};
    Object.keys(gaugeParameters).forEach(function(key){if(!/^Gauge \d+ (?:Current|Max)$/.test(key))return;String(gaugeParameters[key]).replace(/\bv\[(\d+)\]/g,function(match,id){id=Number(id);if($gameVariables._data[id]===undefined||/ Max$/.test(key)&&$gameVariables.value(id)<=0)$gameVariables.setValue(id,/ Max$/.test(key)?100:0);gaugeInputs[id]=$gameVariables.value(id);return match;});});
    var actor=$gameActors.actor(1);actor.changeClass(2,false);actor.changeLevel(3,false);actor.recoverAll();
    report.preparation={difficultyVariable:4980,difficultyValue:4,miasmaLimitVariable:1259,miasmaLimit:100,miasmaVariable:1030,miasma:18,actorClass:2,actorLevel:3};
    report.preparation.nativeGaugeInputs=gaugeInputs;
    var commandPrelude=null;
    $dataCommonEvents.some(function(ce){if(!ce)return false;return ce.list.some(function(c,i){if(c.code!==355||!/^var a\s*=\s*\[0,0,0,0,0,0,0,0,0\];$/.test(c.parameters[0]))return false;for(var j=i+1;j<Math.min(i+16,ce.list.length)&&ce.list[j].code===655;j++){if(ce.list[j].parameters[0]==='window.eroCommand = a;'){commandPrelude={event:ce.id,first:i,last:j,list:ce.list.slice(i,j+1)};return true;}}return false;});});
    if(commandPrelude){
        var prelude=commandPrelude.list,bootstrap=new Game_Interpreter();
        check('native pre-battle command initializer located',prelude[0].code===355&&prelude[prelude.length-1].parameters[0]==='window.eroCommand = a;');
        bootstrap.setup(prelude.concat([{code:0,indent:0,parameters:[]}]),0);for(var init=0;bootstrap.isRunning()&&init<10;init++)bootstrap.update();
        check('native pre-battle command initializer completes',!bootstrap.isRunning()&&Array.isArray(window.eroCommand)&&window.eroCommand.length===9);
        report.preparation.nativeCommandPrelude={commonEvent:commandPrelude.event,firstCommand:commandPrelude.first,lastCommand:commandPrelude.last};
    }
    check('native transformed actor has a valid MP gauge',actor.mmp>0);
    var catchBattle=SceneManager.catchException;SceneManager.catchException=function(e){catchBattle.call(this,e);finish(e);};
    var gauge=Window_Base.prototype.drawGauge;Window_Base.prototype.drawGauge=function(){if(Array.prototype.slice.call(arguments,0,4).some(function(n){return !isFinite(n);}))report.gauge={args:Array.prototype.slice.call(arguments),members:$gameParty.battleMembers().map(function(a){return{id:a.actorId(),mp:a.mp,mmp:a.mmp,hp:a.hp,mhp:a.mhp,rate:a.mpRate()};})};return gauge.apply(this,arguments);};
    canonical=JSON.stringify([$dataEnemies,$dataStates,$dataSystem,$dataSkills]);
    SceneManager.requestUpdate=function(){};if(SceneManager.isGameActive)SceneManager.isGameActive=function(){return true;};
    report.troopId=Scene_Battle.prototype.commandEnemyStatus?40:151;
    if(!Scene_Battle.prototype.commandEnemyStatus){DataManager.loadMapData($dataSystem.startMapId);phase='map';}
    else{BattleManager.setup(report.troopId,false,true);SceneManager.goto(Scene_Battle);phase='battle';}
   }else if(phase==='map'){
    if(!DataManager.isMapLoaded())return;$gameMap.setup($dataSystem.startMapId);$gameMap.changeBattleback('Castle1','frame');report.preparation.nativeMapSetup=$dataSystem.startMapId;report.preparation.nativeBattlebacks=['Castle1','frame'];
    BattleManager.setup(report.troopId,false,true);SceneManager.goto(Scene_Battle);phase='battle';
   }else if(phase==='battle'){
    scene=SceneManager._scene;if(!scene||scene.constructor.name!=='Scene_Battle'||!scene.isReady()||!SceneManager._sceneStarted)return;

    object=scene._objInfoWindow;anchor=scene._skillWindow;
    check('native skill tooltip exists',!!object&&!!anchor);
    var actor=$gameActors.actor(1),selected=[64,65,54,1,15,47,92,91];selected.forEach(function(id){if($dataSkills[id])actor.learnSkill(id);});
    anchor.setActor(actor);anchor.setStypeId($dataSkills[64].stypeId);anchor.refresh();anchor.show();anchor.activate();object._showInfo=true;
    // Native skillMpCost lazily initializes mpCostPer for unopened skills.
    // Complete that normal engine initialization before freezing the database.
    $dataSkills.forEach(function(s){if(s)actor.skillMpCost(s);});
    report.preparation.nativeMpCostCacheInitializedForAllSkills=true;
    report.variant=CelesphoniaEnglishMeta.version;report.cases=[];report.nativeSkillData={};
    report.nativeSnapshot=JSON.stringify([$dataSkills,$dataSystem,$dataStates,$dataCommonEvents,$gameVariables._data,$gameSwitches._data,actor._skills,$gameParty._items]);
    function rowData(){var r={rows:object._data.slice(),top:object._textTop.slice(),bottom:object._textBottom.slice(),damage:object._damageData.slice(),cost:object._lastData.slice(),customCost:object._costData.slice(),requirements:object._requireData.slice()};['_stateData','_stateTitle','_stateRequire','_stateAfter'].forEach(function(k){if(object[k])r[k]=JSON.parse(JSON.stringify(object[k]));});return r;}
    function snapshot(skill){object.setItem(null,anchor);anchor.setStypeId(skill.stypeId);anchor.refresh();anchor.select(Math.max(0,anchor._data.indexOf(skill)));object.setItem(skill,anchor);object.openness=255;var r=rowData();r.id=skill.id;r.name=CelesphoniaEnglish.translate(skill.name);r.x=object.x;r.y=object.y;r.width=object.width;r.height=object.height;r.bitmapHeight=object.contents.height;r.viewHeight=object.height-object.standardPadding()*2;r.font=object.standardFontSize();return r;}
    function allRows(c){var out=[];function strings(v){if(typeof v==='string')out.push(v);else if(Array.isArray(v))v.forEach(strings);}['rows','top','bottom','damage','cost','customCost','requirements','_stateData','_stateTitle','_stateRequire','_stateAfter'].forEach(function(k){strings(c[k]);});return out;}
    function numeric(c){return allRows(c).map(plain).join('\n').match(/[+-]?\d+(?:\.\d+)?%?/g)||[];}
    selected.forEach(function(id){var s=$dataSkills[id];if(!s)return;report.nativeSkillData[id]={name:s.name,damage:s.damage,effects:s.effects,scope:s.scope,occasion:s.occasion,note:s.note};report.cases.push(snapshot(s));});
    report.primary=snapshot($dataSkills[92]);report.primaryImage=capture('barrier',{x:object.x,y:object.y,width:object.width,height:object.height});
    report.fullImage=capture('full');
    var glyphs=new Bitmap(object.contents.width,object.lineHeight());
    var targetRow=object._data.map(plain).map(function(t){return t.indexOf('100')>=0&&(/Inflicts|附加|Add State/.test(t));}).indexOf(true);
    if(targetRow>=0){var textY=object.titleHeight()+object.exDataHeight(object._textTop)+object.textPadding()+object.lineHeight()*targetRow;glyphs.blt(object.contents,0,textY,glyphs.width,glyphs.height,0,0);report.glyphImage=out+'.glyph.png';fs.writeFileSync(report.glyphImage,new Buffer(glyphs._canvas.toDataURL('image/png').split(',')[1],'base64'));}
    var non=typeof DataManager.isState==='function'&&typeof object.getStateCul==='function'?$dataStates[369]:$dataWeapons.filter(function(s){return s&&s.name&&s.params.some(function(n){return n!==0;});})[0];
    object.setItem(null,anchor);object.setItem(non,anchor);report.nonSkill=rowData();
    report.sweep={records:0,named:0,generated:0,errors:[],residuals:[],rows:[],effectCodes:{}};
    $dataSkills.forEach(function(s){if(!s)return;report.sweep.records++;if(s.name&&s.name.trim())report.sweep.named++;(s.effects||[]).forEach(function(e){report.sweep.effectCodes[e.code]=(report.sweep.effectCodes[e.code]||0)+1;});
      try{object._item=s;object.makeObjInfo();var c=rowData();c.id=s.id;c.name=s.name;c.named=!!(s.name&&s.name.trim());report.sweep.rows.push(c);report.sweep.generated++;
       allRows(c).forEach(function(text){if(/[\u3040-\u30ff\u3400-\u9fff]/.test(plain(text)))report.sweep.residuals.push({id:s.id,name:s.name,named:c.named,text:text});});
      }catch(e){report.sweep.errors.push({id:s.id,name:s.name,error:String(e.message||e)});}
    });
    report.renderSnapshotUnchanged=report.nativeSnapshot===JSON.stringify([$dataSkills,$dataSystem,$dataStates,$dataCommonEvents,$gameVariables._data,$gameSwitches._data,actor._skills,$gameParty._items]);
    if(!report.renderSnapshotUnchanged){report.renderDiff=[];function diff(a,b,p){if(report.renderDiff.length>=24||JSON.stringify(a)===JSON.stringify(b))return;if(a&&b&&typeof a==='object'&&typeof b==='object'){Object.keys(a).concat(Object.keys(b).filter(function(k){return !a.hasOwnProperty(k);})).forEach(function(k){diff(a[k],b[k],p+'.'+k);});}else report.renderDiff.push({path:p,before:a,after:b});}diff(JSON.parse(report.nativeSnapshot),[$dataSkills,$dataSystem,$dataStates,$dataCommonEvents,$gameVariables._data,$gameSwitches._data,actor._skills,$gameParty._items],'native');}
    if(fixed){
      var before=JSON.parse(fs.readFileSync(out.replace(/after\.json$/,'before.json'),'utf8'));
      check('native skill effects, formulas and identifiers unchanged',JSON.stringify(report.nativeSkillData)===JSON.stringify(before.nativeSkillData));
      check('non-skill tooltip output unchanged',JSON.stringify(report.nonSkill)===JSON.stringify(before.nonSkill));
      check('all skill records enumerated',report.sweep.records===$dataSkills.filter(Boolean).length&&report.sweep.records===before.sweep.records);
      check('no new native formatter exceptions',JSON.stringify(report.sweep.errors)===JSON.stringify(before.sweep.errors));
      var expected={};before.sweep.rows.forEach(function(c){expected[c.id]=numeric(c);});
      check('all generated skill numeric fields preserved',report.sweep.rows.every(function(c){return JSON.stringify(numeric(c))===JSON.stringify(expected[c.id]);}));
      var oldDamage={};before.sweep.rows.forEach(function(c){oldDamage[c.id]=c.damage;});check('all damage and formula rows remain exact',report.sweep.rows.every(function(c){return JSON.stringify(c.damage)===JSON.stringify(oldDamage[c.id]);}));
      check('all generated skill fields have no unexplained CJK residuals',report.sweep.residuals.length===0,report.sweep.residuals.slice(0,10));
      report.cases.forEach(function(c){check('representative bounds '+c.id,c.x>=0&&c.y>=0&&c.x+c.width<=Graphics.boxWidth+1&&c.y+c.height<=Graphics.boxHeight+1);check('representative text English '+c.id,!/[\u3040-\u30ff\u3400-\u9fff]/.test(allRows(c).map(plain).join('\n')),allRows(c));});
      var barrier=allRows(report.primary).map(plain).join('\n');
      check('native variant buff names localized',(report.nativeSkillData[92].effects||[]).filter(function(e){return e.code===21;}).every(function(e){return barrier.indexOf(CelesphoniaEnglish.translate($dataStates[e.dataId].name))>=0;}),barrier);
      if(report.variant==='three-in-one-3-3-5-v1-061')check('three-in-one DEF/MDF buff names localized',barrier.indexOf('DEF Boost (Major)')>=0&&barrier.indexOf('MDF Boost (Major)')>=0,barrier);
      if((report.nativeSkillData[92].effects||[]).some(function(e){return e.code===44&&e.dataId===1093;}))check('instant-skill event label localized',/Instant (?:Skill|Spell|Cast)/.test(barrier),barrier);
      var originalW=Graphics.boxWidth,originalH=Graphics.boxHeight;Graphics.boxWidth=320;Graphics.boxHeight=300;var small=snapshot($dataSkills[92]);report.small=small;
      check('small viewport bounds',small.x>=0&&small.y>=0&&small.x+small.width<=320&&small.y+small.height<=300&&small.font>=16);
      if(small.bitmapHeight>small.viewHeight){TouchInput._mouseOverX=object.x+20;TouchInput._mouseOverY=object.y+20;TouchInput._x=TouchInput._mouseOverX;TouchInput._y=TouchInput._mouseOverY;TouchInput._wheelY=120;var index=anchor.index(),top=anchor.topRow();anchor.processWheel();object.update();TouchInput._wheelY=0;check('hover scrolling does not move skill list',object.origin.y>0&&anchor.index()===index&&anchor.topRow()===top);}
      Graphics.boxWidth=originalW;Graphics.boxHeight=originalH;object.setItem(null,anchor);check('empty selection clears tooltip',!object._item&&!object._celesphoniaEnglishSkillInfo);snapshot($dataSkills[92]);check('reopened tooltip starts at first row',object.origin.y===0);
    }
    check('rendering preserves native database and prepared battle state',report.renderSnapshotUnchanged);
    report.sourceDatabaseHashDeferredToRunner=true;delete report.nativeSnapshot;finish();
   }
  }catch(e){finish(e);}
 },70);
})();
