(function(){
 'use strict';var fs=require('fs'),out=__OUTPUT__,done=false,start=Date.now(),report={checks:[],userSavesLoaded:false,userSavesWritten:false,fullGameplayVerified:false};
 function check(name,ok){report.checks.push({name:name,pass:!!ok});if(!ok)throw Error(name);}
 function finish(e){if(done)return;done=true;report.status=e?'FAIL':'PASS';report.error=e&&String(e.stack||e);fs.writeFileSync(out,JSON.stringify(report,null,2));nw.App.quit();}
 var timer=setInterval(function(){if(Date.now()-start>35000){clearInterval(timer);finish(Error('Native startup timeout'));return;}if(!window.SceneManager||!SceneManager._scene||SceneManager._scene.constructor.name!=='Scene_Title')return;clearInterval(timer);
 try{
  check('native title startup and English without controls',!!window.CelesphoniaEnglish&&!window.CelesphoniaControls);
  check('latest version-specific pack loaded',CelesphoniaEnglishMeta.version==='three-in-one-3-3-5-v1-061');
  DataManager.createGameObjects();$gameParty.setupStartingMembers();var db=JSON.stringify([$dataSystem,$dataSkills,$dataCommonEvents]),win=new Window_Base(0,0,1000,400);
  var fields=[$dataSkills[737].name,$dataSkills[737].description,$dataSkills[1043].name,$dataSkills[1043].description,$dataSkills[1106].description,$dataSkills[1124].description,$dataSkills[1164].name,$dataSkills[1164].description,$dataItems[787].description];
  fields.forEach(function(text,i){var rendered=win.convertEscapeCharacters(text);check('new native display field '+i,rendered!==text&&!/[\u3400-\u9fff]/.test(rendered));});
  check('name and anal-label repairs retained',CelesphoniaEnglish.translate($dataSkills[955].name)==='Extreme Sensitivity (Anal)');
  check('ordinary movement has no menu lock',!window.CelesphoniaControls&&typeof Game_Player.prototype.canMove==='function');
  check('native database unchanged',db===JSON.stringify([$dataSystem,$dataSkills,$dataCommonEvents]));
  report.nativeVersion=CelesphoniaEnglishMeta.version;finish();
 }catch(e){finish(e);}},100);
})();
