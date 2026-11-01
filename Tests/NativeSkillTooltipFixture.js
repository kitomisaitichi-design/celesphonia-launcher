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
    var actor=$gameActors.actor(1);[64,65,54,1,15,47].forEach(function(id){if($dataSkills[id])actor.learnSkill(id);});
    anchor.setActor(actor);anchor.setStypeId($dataSkills[64].stypeId);anchor.refresh();anchor.show();anchor.activate();
    report.variant=CelesphoniaEnglishMeta.version;report.cases=[];report.nativeSkillData={};
    object._showInfo=true;report.originalSettings=JSON.parse(JSON.stringify(object._windowSet));
    report.nativeSnapshot=JSON.stringify([$dataSkills,$dataSystem,$dataStates,$gameVariables._data,$gameSwitches._data,actor._skills,$gameParty._items]);
    canonical=JSON.stringify([$dataEnemies,$dataStates,$dataSystem,$dataSkills]);
    function snapshot(skill){
      object.setItem(null,anchor);anchor.setStypeId(skill.stypeId);anchor.refresh();anchor.select(Math.max(0,anchor._data.indexOf(skill)));object.setItem(skill,anchor);object.openness=255;
      return {id:skill.id,name:CelesphoniaEnglish.translate(skill.name),rows:object._data.slice(),damage:object._damageData.slice(),cost:object._lastData.slice(),customCost:object._costData.slice(),x:object.x,y:object.y,width:object.width,height:object.height,bitmapWidth:object.contents.width,bitmapHeight:object.contents.height,viewHeight:object.height-object.standardPadding()*2,font:object.standardFontSize(),layout:object._celesphoniaEnglishSkillInfo||null};
    }
    [64,65,54,1,15,47].forEach(function(id){var skill=$dataSkills[id];if(!skill)return;report.nativeSkillData[id]={name:skill.name,damage:JSON.parse(JSON.stringify(skill.damage)),effects:skill.effects,scope:skill.scope,occasion:skill.occasion};report.cases.push(snapshot(skill));});
    report.primary=snapshot($dataSkills[64]);report.iceImage=capture('ice',{x:object.x,y:object.y,width:object.width,height:object.height});
    var damageY=object.standardPadding()+object.titleHeight()+object.exDataHeight(object._textTop)+object.dataHeight();
    report.damageImage=capture('damage',{x:object.x,y:object.y+damageY,width:object.width,height:object.lineHeight()*object._damageData.length+object.textPadding()*2});
    var textLayer=new Bitmap(object.contents.width,object.lineHeight()*object._damageData.length+object.textPadding()*2);
    textLayer.blt(object.contents,0,damageY-object.standardPadding(),textLayer.width,textLayer.height,0,0);
    report.damageTextImage=out+'.damage-text.png';fs.writeFileSync(report.damageTextImage,new Buffer(textLayer._canvas.toDataURL('image/png').split(',')[1],'base64'));
    var label=new Bitmap(108,object.lineHeight()+3);label.blt(object.contents,0,damageY-object.standardPadding()+object.textPadding()+object.lineHeight(),label.width,label.height,0,0);
    report.labelImage=out+'.label.png';fs.writeFileSync(report.labelImage,new Buffer(label._canvas.toDataURL('image/png').split(',')[1],'base64'));
    if(captureOnly){report.captureOnly=true;finish();return;}
    report.primaryAll=snapshot($dataSkills[65]);report.allImage=capture('all',{x:object.x,y:object.y,width:object.width,height:object.height});
    var otherItem=typeof DataManager.isState==='function'&&typeof object.getStateCul==='function'?$dataStates[369]:$dataWeapons.filter(function(w){return w&&w.name&&w.params.some(function(n){return n!==0;});})[0];
    object.setItem(null,anchor);object.setItem(otherItem,anchor);report.nonSkill={rows:object._data.slice(),damage:object._damageData.slice(),width:object.width,height:object.height,x:object.x,y:object.y};
    if(fixed){
      var before=JSON.parse(fs.readFileSync(out.replace(/after\.json$/,'before.json'),'utf8'));
      check('native values, effects and formulas equal baseline',JSON.stringify(report.nativeSkillData)===JSON.stringify(before.nativeSkillData));
      check('existing state/equipment tooltip remains unchanged',JSON.stringify(report.nonSkill)===JSON.stringify(before.nonSkill));
      report.cases.forEach(function(c){
        var all=c.rows.concat(c.damage,c.cost,c.customCost).map(plain).join('\n');
        if(c.id===47){
          // Native dense-skill metadata contains already-corrupt state names.
          // Keep those exact unknown values; verify translated field captions
          // and numeric parity, rather than guessing their intended names.
          check('dense tooltip field captions are English',c.rows.concat(c.damage).map(plain).every(function(row){return !/[\u3400-\u9fff]/.test(row.split(':')[0]);}),c.rows);
          report.nativeDenseNameLimitation=true;
        }else check('English skill labels and values '+c.id,!/[\u3400-\u9fff]/.test(all),all);
        var original=before.cases.filter(function(row){return row.id===c.id;})[0];
        function values(row){return row.rows.concat(row.damage,row.cost).map(plain).join('\n').match(/[+-]?\d+(?:\.\d+)?%?/g)||[];}
        check('all displayed numeric values preserved '+c.id,JSON.stringify(values(c))===JSON.stringify(values(original)));
        check('skill tooltip fits viewport '+c.id,c.x>=0&&c.y>=0&&c.x+c.width<=Graphics.boxWidth+1&&c.y+c.height<=Graphics.boxHeight+1,c);
        var native=report.nativeSkillData[c.id];
        if(native.damage.type>0)check('exact displayed native formula '+c.id,plain(c.damage.join('')).replace(/\s/g,'').indexOf(native.damage.formula.replace(/\s/g,''))>=0,c.damage);
      });
      check('natural occasion wording',plain(report.primary.rows.join('\n')).indexOf('Usable: In Battle')>=0);
      check('absent optional custom cost is not displayed as undefined',report.cases.every(function(c){return c.customCost.join('').indexOf('undefined')<0;}));
      check('ice element and formula label localized',plain(report.primary.damage.join('\n')).indexOf('Ice')>=0&&plain(report.primary.damage.join('\n')).indexOf('Formula: ')>=0);
      snapshot($dataSkills[64]);var costs=object._costData.slice();object.getObjCustomDisplayCost({customCostText:'HP x 2'});
      var customEnabled=Imported.YEP_SkillCore&&object.getObjTextShow('OIWSkillCostText','CustomCost');
      check('real optional custom cost honors native visibility',customEnabled?plain(object._costData.join('\n')).indexOf('HP x 2')>=0:JSON.stringify(object._costData)===JSON.stringify(costs));object._costData=costs;
      var width=Graphics.boxWidth,height=Graphics.boxHeight;Graphics.boxWidth=320;Graphics.boxHeight=300;
      var small=snapshot($dataSkills[65]);report.small=small;
      check('small viewport bounds',small.x>=0&&small.y>=0&&small.x+small.width<=320&&small.y+small.height<=300,small);
      check('small viewport text is readable and fully retained',small.font>=16&&small.layout&&small.layout.sourceDamage.join('').replace(/\s/g,'').indexOf($dataSkills[65].damage.formula.replace(/\s/g,''))>=0,small);
      if(small.bitmapHeight>small.viewHeight){
        TouchInput._mouseOverX=object.x+20;TouchInput._mouseOverY=object.y+20;TouchInput._x=TouchInput._mouseOverX;TouchInput._y=TouchInput._mouseOverY;TouchInput._wheelY=120;
        var index=anchor.index(),top=anchor.topRow();anchor.processWheel();object.update();TouchInput._wheelY=0;
        check('overflow tooltip wheel belongs to tooltip',object.origin.y>0&&anchor.index()===index&&anchor.topRow()===top);
      }
      Graphics.boxWidth=width;Graphics.boxHeight=height;object.setItem(null,anchor);check('empty selection clears',!object._item&&!object._celesphoniaEnglishSkillInfo);
      snapshot($dataSkills[64]);check('selection reopens from first row',object.origin.y===0);
      check('rendering does not mutate native database or battle state',report.nativeSnapshot===JSON.stringify([$dataSkills,$dataSystem,$dataStates,$gameVariables._data,$gameSwitches._data,actor._skills,$gameParty._items]));
      check('native tooltip settings unchanged',JSON.stringify(report.originalSettings)===JSON.stringify(object._windowSet));
    }
    check('original database remains unchanged',canonical===JSON.stringify([$dataEnemies,$dataStates,$dataSystem,$dataSkills]));
    delete report.nativeSnapshot;finish();
   }
  }catch(e){finish(e);}
 },70);
})();
