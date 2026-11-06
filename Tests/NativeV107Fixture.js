(function(){
 'use strict';var fs=require('fs'),out=__OUTPUT__,before=__CAPTURE_ONLY__,done=false,start=Date.now();
 var report={checks:[],cases:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function check(name,ok,detail){report.checks.push({name:name,pass:!!ok,detail:detail});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 function capture(item,label){
  var help=new Window_Help(2);help.setItem(item);help.update();
  var stage=new PIXI.Container(),layer=new WindowLayer();layer.move(0,0,Graphics.boxWidth,Graphics.boxHeight);stage.addChild(layer);layer.addChild(help);
  var frame=Bitmap.snap(stage),image=new Bitmap(help.width,help.height+38);image.fillAll('#142961');image.fontFace=help.contents.fontFace;image.fontSize=22;image.textColor='#e8efff';image.drawText(label,16,2,image.width-32,32);image.blt(frame,0,0,help.width,help.height,0,38);
  var file=out+'.'+label.replace(/[^a-z0-9]+/gi,'-')+'.png';fs.writeFileSync(file,new Buffer(image._canvas.toDataURL('image/png').split(',')[1],'base64'));layer.removeChild(help);return file;
 }
 var timer=setInterval(function(){if(Date.now()-start>35000){clearInterval(timer);finish(Error('Native startup timeout'));return;}if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;clearInterval(timer);
 try{
  check('English active without runtime controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls);
  CelesphoniaEnglish.captureSeen=false;check('1.07 metadata active',CelesphoniaEnglishMeta.version==='three-in-one-3-3-5-v1-07');
  DataManager.createGameObjects();$gameParty.setupStartingMembers();var actor=$gameActors.actor(1);
  [39,46,102,103,122,238,571,1055,1056,1057,1058,1059].forEach(function(id){actor.skillMpCost($dataSkills[id]);});
  var snapshot=JSON.stringify([$dataSystem,$dataSkills,$dataCommonEvents,$dataItems,$dataStates,$dataWeapons,$dataArmors]),win=new Window_Base(0,0,1000,400);
  var fields=window.V107ReviewedNativeFields;check('complete changed-text fixture loaded',Array.isArray(fields)&&fields.length===56);
  report.changedFields=fields.map(function(source){var text=win.convertEscapeCharacters(source);return {source:source,rendered:text,translated:text!==source,remainingCJK:/[\u3400-\u9fff]/.test(text)};});
  if(!before)report.changedFields.forEach(function(row,i){check('new/changed native field '+i,!row.remainingCJK,row.rendered);});
  [[$dataItems[15],'Vitality-Potion-I'],[$dataItems[46],'Power-Sigil-I'],[$dataSkills[238],'Miasma-Adaptation'],[$dataSkills[1059],'Storm-Pierce']].forEach(function(row){report.cases.push({label:row[1],source:row[0].description,image:capture(row[0],row[1])});});
  if(!before){
   check('Miasma cap warning rendered',CelesphoniaEnglish.translate($dataSkills[238].description).indexOf('cap')>=0);
   check('Abyssphonia half damage warning rendered',CelesphoniaEnglish.translate($dataSkills[1059].description).indexOf('half damage')>=0);
   var oldScar=ConfigManager.noScar,choices=['伤痕:开 if(!ConfigManager.noScar)','伤痕:关 if(ConfigManager.noScar)'];
   try{[false,true].forEach(function(hidden){ConfigManager.noScar=hidden;$gameMessage.clear();var interpreter=new Game_Interpreter(),params=[choices,-1,-1,2,0];interpreter.setup([{code:102,indent:0,parameters:params},{code:404,indent:0,parameters:[]}],0);interpreter.setupChoices(params);var shown=$gameMessage.choices(),rendered=shown.map(function(text){return win.convertEscapeCharacters(text);});check('native scar choice guard '+hidden,rendered.length===1&&!/[\u3400-\u9fff]/.test(rendered[0])&&rendered[0]===(hidden?'Scars: Off':'Scars: On'),rendered);});}finally{ConfigManager.noScar=oldScar;$gameMessage.clear();}
   check('bedroom display guard retained',!!Game_Map.prototype.update._celesphoniaSeparateWardrobe);
   check('Memory Library icon expansion retained',win.convertEscapeCharacters('\\iw[160]').indexOf('Lightning Magic')>=0);
  }
  check('stored native item occasion values preserved',$dataItems[51].occasion===1&&$dataItems[56].occasion===0);
  check('native databases unchanged by display',snapshot===JSON.stringify([$dataSystem,$dataSkills,$dataCommonEvents,$dataItems,$dataStates,$dataWeapons,$dataArmors]));
  report.nativeVersion=CelesphoniaEnglishMeta.version;report.before=before;finish();
 }catch(e){finish(e);}},100);
})();
