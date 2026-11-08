(function(){
 'use strict';var fs=require('fs'),output=__OUTPUT__,start=Date.now(),finished=false,report={status:'PASS',checks:[],userSavesWritten:false};
 function check(name,ok,value){report.checks.push({name:name,pass:!!ok,value:value});if(!ok)throw Error(name+': '+JSON.stringify(value));}
 function finish(error){if(finished)return;finished=true;if(error){report.status='FAIL';report.error=String(error.stack||error);}fs.writeFileSync(output,JSON.stringify(report,null,2));nw.App.quit();}
 var timer=setInterval(function(){if(Date.now()-start>35000){clearInterval(timer);return finish(Error('Startup timed out'));}if(window.SceneManager&&SceneManager._scene&&SceneManager._scene.constructor.name==='Splash_Screen'&&DataManager.isDatabaseLoaded())SceneManager.goto(Scene_Title);if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title'||!window.CelesphoniaLocalTranslation||CelesphoniaLocalTranslation.status().entries<1)return;clearInterval(timer);
  try{
   DataManager.setupNewGame();var before=JSON.stringify([$dataSystem,$dataActors,$dataCommonEvents,$gameVariables._data]);var interpreter=new Game_Interpreter();interpreter.pluginCommand('D_TEXT_SETTING',['BG_COLOR','black']);var color=$gameScreen.dTextBackColor;new Bitmap(20,20).gradientFillRect(0,0,20,20,'rgba(0,0,0,0)',color);
   check('plugin color parser remains native',color==='black',color);check('raw escape converter remains native',SceneManager.getHiddenWindow().convertEscapeCharacters('black')==='black');
   check('shared renderer active',!!(window.CelesphoniaEnglish&&CelesphoniaEnglish._runtimeLoaded));check('user override reaches current shared adapters',CelesphoniaEnglish.translate('物品')==='Objets (révisés)',CelesphoniaEnglish.translate('物品'));
   var term=window.GlossaryFixtureTerm||'\\I[2518]\\c[27]媚药侵染(超敏感)',expected=window.GlossaryFixtureTarget||'\\I[2518]\\c[27]Imprégnation aphrodisiaque (hypersensibilité)';check('imported exact dictionary target reaches shared adapters',CelesphoniaEnglish.translate(term)===expected,CelesphoniaEnglish.translate(term));
   var command=Object.create(Window_Command.prototype);command._list=[];command.addCommand('物品','native-symbol',true);check('native command name/symbol unchanged',command._list[0].name==='物品'&&command._list[0].symbol==='native-symbol');
   check('unknown and technical source unchanged',CelesphoniaEnglish.translate('black')==='black'&&CelesphoniaEnglish.translate('$gameVariables.value(1)')==='$gameVariables.value(1)');check('gameplay data unchanged',before===JSON.stringify([$dataSystem,$dataActors,$dataCommonEvents,$gameVariables._data]));
   report.provider=CelesphoniaLocalTranslation.status();finish();
  }catch(e){finish(e);}
 },200);
})();
