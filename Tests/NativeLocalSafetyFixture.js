(function(){
 'use strict';var fs=require('fs'),output=__OUTPUT__,fixed=__FIXED__,start=Date.now(),finished=false,report={status:'PASS',checks:[],userSavesWritten:false};
 function check(name,ok,value){report.checks.push({name:name,pass:!!ok,value:value});if(!ok)throw Error(name+': '+JSON.stringify(value));}
 function finish(error){if(finished)return;finished=true;if(error){report.status='FAIL';report.error=String(error.stack||error);}fs.writeFileSync(output,JSON.stringify(report,null,2));nw.App.quit();}
 var timer=setInterval(function(){if(Date.now()-start>40000){clearInterval(timer);return finish(Error('Startup timed out'));}if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title'||!window.CelesphoniaLocalTranslation||CelesphoniaLocalTranslation.status().entries<100)return;clearInterval(timer);
  try{
   DataManager.setupNewGame();if(fs.existsSync('__SAVE__'))DataManager.extractSaveContents(JsonEx.parse(LZString.decompressFromBase64(fs.readFileSync('__SAVE__','utf8'))));var before=JSON.stringify([$dataSystem,$dataActors,$dataCommonEvents,$gameVariables._data]),interpreter=new Game_Interpreter();interpreter.pluginCommand('D_TEXT_SETTING',['BG_COLOR','black']);var color=$gameScreen.dTextBackColor,error=null;
   try{new Bitmap(20,20).gradientFillRect(0,0,20,20,'rgba(0,0,0,0)',color);}catch(e){error=String(e);}
   report.color=color;report.colorError=error;
   if(fixed){check('plugin color parsing stays native',color==='black'&&!error,color);check('raw converter stays native',SceneManager.getHiddenWindow().convertEscapeCharacters('black')==='black');check('shared adapters loaded',!!(window.CelesphoniaEnglish&&CelesphoniaEnglish._runtimeLoaded));check('manual dictionary edit reaches shared renderer',CelesphoniaEnglish.translate('物品')==='Objets (révisés)',CelesphoniaEnglish.translate('物品'));var command=Object.create(Window_Command.prototype);command._list=[];command.addCommand('物品','native-symbol',true);check('command names and symbols remain native',command._list[0].name==='物品'&&command._list[0].symbol==='native-symbol');}
   else check('reproduced reported noir gradient crash',color==='noir'&&/color/.test(error||''),error);
   check('native gameplay data unchanged',before===JSON.stringify([$dataSystem,$dataActors,$dataCommonEvents,$gameVariables._data]));
   $gameScreen.dTextBackColor='black';SceneManager.goto(Scene_Menu);
   setTimeout(function(){try{var frame=Bitmap.snap(SceneManager._scene);report.image=output+'.png';fs.writeFileSync(report.image,new Buffer(frame._canvas.toDataURL('image/png').split(',')[1],'base64'));finish();}catch(e){finish(e);}},1200);
  }catch(e){finish(e);}
 },200);
})();
