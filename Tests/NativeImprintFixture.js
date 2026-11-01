(function(){
 'use strict';
 var fs=require('fs'),out=__OUTPUT__,fixed=__FIXED__,captureOnly=__CAPTURE_ONLY__,done=false,began=Date.now(),phase='boot',report={checks:[],cases:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false},canonical;
 function check(name,ok){report.checks.push({name:name,pass:!!ok});if(!ok)throw Error(name);}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 function image(scene,suffix){var visible=scene.children.map(function(child){return [child,child.visible];});scene.children.forEach(function(child){if(child!==scene._windowLayer)child.visible=false;});var b;try{b=Bitmap.snap(scene);}finally{visible.forEach(function(pair){pair[0].visible=pair[1];});}var file=out+'.'+suffix+'.png';fs.writeFileSync(file,new Buffer(b._canvas.toDataURL('image/png').split(',')[1],'base64'));return file;}
 function capture(win,item){var calls=[],draw=Bitmap.prototype.drawText;Bitmap.prototype.drawText=function(text,x,y,width,height){if(this===win.contents)calls.push({text:String(text),x:x,y:y,width:this.measureTextWidth(String(text)),font:this.fontSize,bottom:y+height});return draw.apply(this,arguments);};try{win.setItem(null);win.setItem(item);}finally{Bitmap.prototype.drawText=draw;}var layout=win._celesphoniaEnglishImprintLayout;return {id:item.id,name:CelesphoniaEnglish.translate(item.name),cjk:calls.some(function(c){return /[\u3400-\u9fff]/.test(c.text);}),right:Math.max.apply(Math,calls.map(function(c){return c.x+c.width;})),available:win.contentsWidth(),bottom:Math.max.apply(Math,calls.map(function(c){return c.bottom;})),bitmapHeight:win.contents.height,layout:layout};}
 window.addEventListener('error',function(e){finish(e.error||Error(e.message));});
 var clock=setInterval(function(){if(!done&&phase!=='boot')SceneManager.update();},16);
 var timer=setInterval(function(){
  if(done)return;if(Date.now()-began>40000){finish(Error('Native imprint UI timeout'));return;}
  try{
   if(phase==='boot'){
    if(!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;
    check('English enabled without runtime controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls);
    DataManager.createGameObjects();$gameParty.setupStartingMembers();var actor=$gameActors.actor(1);$gameParty.setMenuActor(actor);actor.addMagicSlot(4-actor.magicSlots());
    [115,101,131].forEach(function(id,i){$gameParty.gainItem($dataWeapons[id],1);actor.changeMagic(i,$dataWeapons[id]);});
    canonical=JSON.stringify([$dataSkills,$dataWeapons,$dataSystem]);
    SceneManager.requestUpdate=function(){};if(SceneManager.isGameActive)SceneManager.isGameActive=function(){return true;};
    new Game_Interpreter().pluginCommand('callMagicEquip',['1']);phase='scene';
   }else if(phase==='scene'){
    var scene=SceneManager._scene;if(!scene||scene.constructor.name!=='Scene_MagicEquip'||!scene._explainWindow||!scene.isReady())return;
    var win=scene._explainWindow;scene.commandEquip();scene._slotWindow.select(2);scene._slotWindow.updateHelp();
    report.shown=capture(win,$dataWeapons[131]);report.beforeAfterImage=image(scene,'shown');
    if(captureOnly){
     var visible=scene.children.map(function(c){return [c,c.visible];});scene.children.forEach(function(c){if(c!==scene._windowLayer)c.visible=false;});
     var nativeFrame;try{nativeFrame=Bitmap.snap(scene);}finally{visible.forEach(function(p){p[0].visible=p[1];});}
     var panel=new Bitmap(win.width,win.height);panel.blt(nativeFrame,win.x,win.y,win.width,win.height,0,0);
     report.panelImage=out+'.panel.png';fs.writeFileSync(report.panelImage,new Buffer(panel._canvas.toDataURL('image/png').split(',')[1],'base64'));report.captureOnly=true;finish();return;
    }
    var actor=$gameActors.actor(1),state=JSON.stringify([actor._magics,$gameParty._weapons,$gameVariables._data,$gameSwitches._data]);
    var items=$dataWeapons.filter(function(item){return item&&$dataSystem.weaponTypes[item.wtypeId]===TMPlugin.MagicSlot.MagicTypeName;});
    items.forEach(function(item){var row=capture(win,item);report.cases.push(row);if(fixed){check('translated imprint description '+item.id,!row.cjk);check('imprint text fits width '+item.id,row.right<=row.available+1);check('imprint text retained in bitmap '+item.id,row.bottom<=row.bitmapHeight+1);}});
    if(fixed){check('long imprint uses readable scrolling',report.cases.some(function(r){return r.layout&&r.layout.scrollable;}));}
    capture(win,$dataWeapons[135]);report.denseImage=image(scene,'dense');
    if(fixed&&win._celesphoniaEnglishImprintLayout.scrollable){TouchInput._x=win.x+20;TouchInput._y=win.y+20;TouchInput._wheelY=120;win.update();check('description scroll responds to hover wheel',win.origin.y>0);TouchInput._wheelY=0;win.origin.y=Math.max(0,win.contents.height-win.contentsHeight());report.denseEndImage=image(scene,'dense-end');}
    win.setItem(null);check('empty selection clears safely',!win.item&&!win.origin.y);
    capture(win,$dataWeapons[131]);win.refresh();check('native state unchanged by description rendering',state===JSON.stringify([actor._magics,$gameParty._weapons,$gameVariables._data,$gameSwitches._data]));
    check('native database unchanged',canonical===JSON.stringify([$dataSkills,$dataWeapons,$dataSystem]));
    scene.commandClear();check('native reset returns all equipped imprints',actor.magics().length===0&&$gameParty.numItems($dataWeapons[131])===1);
    scene._explainWindow.setItem($dataWeapons[131]);check('menu can render again after reset',!!scene._explainWindow.item);
    report.version=CelesphoniaEnglishMeta.version;finish();
   }
  }catch(e){finish(e);}
 },100);
})();
