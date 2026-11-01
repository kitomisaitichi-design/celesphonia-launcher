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
    enemy=$gameTroop.members()[0];check('version-specific native enemy and troop',report.troopId===40?enemy.enemyId()===146&&$gameTroop.members()[1].enemyId()===148:enemy.enemyId()===87);
    enemy.addState(369);var bind=$dataStates.filter(function(s){return s&&s.name==='会使用拘束';})[0];if(bind)enemy.addState(bind.id);
    var pressure=$dataStates.filter(function(s){return s&&/威压/.test(s.name)&&s.priority>=50;})[0];if(pressure)enemy.addState(pressure.id);
    report.enemy={id:enemy.enemyId(),name:enemy.name(),display:CelesphoniaEnglish.translate(enemy.name()),stats:[enemy.hp,enemy.mhp,enemy.mp,enemy.mmp,enemy.atk,enemy.def,enemy.mat,enemy.mdf,enemy.agi],states:enemy.states().map(function(s){return{id:s.id,name:s.name,display:CelesphoniaEnglish.translate(s.name)};})};
    report.viewport={width:Graphics.boxWidth,height:Graphics.boxHeight};report.variant=CelesphoniaEnglishMeta.version;
    info=scene._enemyInfoWindow;states=scene._enemyStateWindow;object=scene._objInfoWindow;
    check('native trait information window exists',!!object);
    report.stateInfoSupported=typeof DataManager.isState==='function'&&typeof object.getStateCul==='function';
    report.tooltipItem=report.stateInfoSupported?$dataStates[369]:$dataWeapons.filter(function(w){return w&&w.name&&w.params.some(function(n){return n!==0;});})[0];
    report.tooltipKind=report.stateInfoSupported?'state':'weapon';
    report.nativeAutoShow=object._showInfo;object._showInfo=true;
    if(info){scene.commandEnemyStatus();report.stats=draws(info,function(){info.refresh();});report.stateRows=draws(states,function(){states.refresh();});anchor=states;}
    else{anchor=scene._enemyWindow;anchor.show();anchor.select(0);report.hypInfoAbsent=true;}
    object.close();object.openness=0;ticks=0;phase='stats';
   }else if(phase==='stats'){
    if(++ticks<8)return;
    if(info){report.statsImage=capture('stats',{x:info.x,y:0,width:info.width,height:Graphics.boxHeight});}
    object.setItem(null,anchor);object.setItem(report.tooltipItem,anchor);object.openness=255;ticks=0;phase='traits';
   }else if(phase==='traits'){
    if(++ticks<3)return;
    report.traits=objectReport();report.traitImage=capture('traits');
    if(captureOnly){report.captureOnly=true;finish();return;}
    if(fixed&&!report.stateInfoSupported){
        var originalTooltip=JSON.parse(fs.readFileSync(out.replace(/after\.json$/,'before.json'),'utf8')).traits;
        check('Maple state-adapter capability gate leaves its native fork unchanged',!object._celesphoniaEnglishStateInfo);
        check('Maple native equipment tooltip has content',object.dataLength()>0);
        check('Maple native tooltip text and geometry match baseline',JSON.stringify([object._data,object.x,object.y,object.width,object.height])===JSON.stringify([originalTooltip.rows,originalTooltip.x,originalTooltip.y,originalTooltip.width,originalTooltip.height]));
        object.setItem(null,anchor);check('Maple empty tooltip selection closes safely',!object._item);object.setItem(report.tooltipItem,anchor);check('Maple native tooltip reopens',object.dataLength()>0);
        check('native database unchanged',canonical===JSON.stringify([$dataEnemies,$dataStates,$dataSystem,$dataSkills]));finish();return;
    }
    if(fixed){
     check('trait panel fits viewport',object.x>=0&&object.y>=0&&object.x+object.width<=Graphics.boxWidth+1&&object.y+object.height<=Graphics.boxHeight+1,report.traits);
     var calls=draws(object,function(){object.refresh();});report.traitDraws=calls;
     check('trait text stays inside its bitmap',calls.every(function(c){return c.x>=0&&c.x+Math.min(c.width,c.limit||c.width)<=object.contents.width+2;}));
    }
    report.nativeTraitRows=object._data.length;
    if(fixed){
     var before=JSON.parse(fs.readFileSync(out.replace(/after\.json$/,'before.json'),'utf8'));
     function numbers(rows){return rows.map(function(s){return plain(s).match(/[+-]?\d+(?:\.\d+)?\s*%?/g)||[];});}
     check('trait values and order unchanged',JSON.stringify(numbers(before.traits.rows))===JSON.stringify(numbers(object._data)));
     if(info){
      var layout=info._celesphoniaEnglishEnemyStats,values=layout.drawAttributes.rows.concat(layout.drawXParams.rows).map(function(row){return row.value;});
      var original=before.stats.filter(function(c){return /^\s*:/.test(c.text);}).map(function(c){return c.text.replace(/^\s*:\s*/,'');});
      check('all eighteen native stat values preserved',values.length===18&&JSON.stringify(values)===JSON.stringify(original),values);
      check('stat values have separate label space',layout.drawAttributes.rows.concat(layout.drawXParams.rows).every(function(row){return row.labelWidth>0&&row.valueWidth>0&&row.fontSize>=16;}));
      check('state titles and duration occupy separate rows',states.itemHeight()>=states.lineHeight()*2);
      scene._enemyListWindow.select(1);check('native enemy selection updates information',info._infoObj===$gameTroop.members()[1]);scene._enemyListWindow.select(0);
      states.setInfoObject(null);check('empty state list clears safely',states.maxItems()===0);states.setInfoObject(enemy);
      var longStates=$dataStates.filter(function(s){return s&&s.priority>=50&&[3,62,76].indexOf(s.id)<0&&!/[\u3400-\u9fff]/.test(CelesphoniaEnglish.translate(s.name));}).sort(function(a,b){return CelesphoniaEnglish.translate(b.name).length-CelesphoniaEnglish.translate(a.name).length;}).slice(0,8);
      var denseStateObject={states:function(){return longStates;},_stateTurns:{}};longStates.forEach(function(s){denseStateObject._stateTurns[s.id]=7;});states.setInfoObject(denseStateObject);
      report.longState={id:longStates[0].id,name:CelesphoniaEnglish.translate(longStates[0].name),height:states.itemHeight()};
      check('long native state names retain their complete wording',states._celesphoniaEnglishStateRows.every(function(row,i){return row.layout.text.replace(/\s/g,'')===CelesphoniaEnglish.translate(longStates[i].name).replace(/\s/g,'');}));
      check('long state names leave a separate duration row',states._celesphoniaEnglishStateRows.every(function(row){return row.layout.height+states.lineHeight()<=states.itemHeight();}));
      states.select(states.maxItems()-1);check('dense native state list scrolls to its final entry',states.topRow()>0);states.setInfoObject(enemy);
     }
     var nativeState=JSON.stringify([$gameVariables._data,$gameSwitches._data,enemy._states,enemy._stateTurns,$gameParty._items,$gameParty._weapons,$gameParty._armors]);
     var width=Graphics.boxWidth,height=Graphics.boxHeight;
     Graphics.boxWidth=640;Graphics.boxHeight=480;object.setItem(null,anchor);object.setItem($dataStates[369],anchor);object.openness=255;
     check('dense trait panel fits a smaller viewport',object.x>=0&&object.y>=0&&object.x+object.width<=640&&object.y+object.height<=480,objectReport());
     check('dense trait text remains scrollable at readable size',object.contents.height>object.height-object.standardPadding()*2&&object.standardFontSize()>=16);
     anchor.activate();if(states)anchor.select(states._data.indexOf($dataStates[369]));object.setItem(null,anchor);object.setItem($dataStates[369],anchor);object.openness=255;
     var index=anchor.index(),top=anchor.topRow();TouchInput._mouseOverX=object.x+20;TouchInput._mouseOverY=object.y+20;TouchInput._x=TouchInput._mouseOverX;TouchInput._y=TouchInput._mouseOverY;TouchInput._wheelY=120;
     anchor.processWheel();report.wheel={input:TouchInput.wheelY,visible:object.visible,openness:object.openness,layout:object._celesphoniaEnglishStateInfo&&{scrollable:object._celesphoniaEnglishStateInfo.scrollable},x:object.x,y:object.y,localX:object.canvasToLocalX(TouchInput._mouseOverX),localY:object.canvasToLocalY(TouchInput._mouseOverY)};object.update();check('hover wheel scrolls trait panel',object.origin.y>0,report.wheel);check('hovering trait panel does not scroll the list beneath',anchor.index()===index&&anchor.topRow()===top);
     TouchInput._wheelY=0;object.origin.y=Math.max(0,object.contents.height-object.height+object.standardPadding()*2);report.denseEndImage=capture('dense-end');
     check('last trait row remains in the retained bitmap',object.origin.y+object.height-object.standardPadding()*2>=object.contents.height-1);
     var end=object.origin.y;TouchInput._mouseOverX=object.x-5;TouchInput._mouseOverY=object.y-5;TouchInput._wheelY=-120;object.update();TouchInput._wheelY=0;check('wheel outside trait panel leaves it unchanged',object.origin.y===end);
     Graphics.boxWidth=width;Graphics.boxHeight=height;object.setItem(null,anchor);check('empty trait selection closes safely',!object._item&&!object._celesphoniaEnglishStateInfo);
     object.setItem($dataStates[369],anchor);object.openness=255;check('trait panel reopens at its first row',object.origin.y===0&&object.x>=0&&object.y>=0);
     check('rendering leaves native battle values unchanged',nativeState===JSON.stringify([$gameVariables._data,$gameSwitches._data,enemy._states,enemy._stateTurns,$gameParty._items,$gameParty._weapons,$gameParty._armors]));
     if(info){scene.onStateListCancel('enemy');check('native cancel returns focus to enemy list',scene._enemyListWindow.active);scene.onStatusHorzCancel('enemy');check('native close restores battle command focus',!info.visible&&scene._actorCommandWindow.active);scene.commandEnemyStatus();check('enemy information reopens',info.visible);}
    }
    check('native database unchanged',canonical===JSON.stringify([$dataEnemies,$dataStates,$dataSystem,$dataSkills]));
    finish();
   }
  }catch(e){finish(e);}
 },70);
})();
